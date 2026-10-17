using InsuranceApi.Data;
using InsuranceApi.Dtos;
using InsuranceApi.Models;
using InsuranceApi.Services;
using Microsoft.EntityFrameworkCore;

namespace InsuranceApi.Tests;

public class AdminQuestionServiceTests
{
    private const string ProductCode = "TESTP";

    // A fresh service and an empty fake database, with one product in it
    private static (AdminQuestionService Service, InsuranceDbContext Db) CreateService()
    {
        var options = new DbContextOptionsBuilder<InsuranceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new InsuranceDbContext(options);
        db.Products.Add(new Product
        {
            CategoryId = 1,
            Code = ProductCode,
            Name = "Test Product",
            CoverageLabel = "Value",
            RatePerThousand = 10,
            MinimumPremium = 50,
            PolicyFee = 10,
            MinCoverage = 1000,
            MaxCoverage = 50000,
            IsActive = true
        });
        db.SaveChanges();

        return (new AdminQuestionService(db), db);
    }

    private static QuestionCreateDto NumberQuestion(string key = "riderAge") => new()
    {
        Key = key,
        Label = "Rider age",
        Type = QuestionType.Number,
        IsRequired = true,
        MinValue = 16,
        MaxValue = 90,
        SortOrder = 1
    };

    private static QuestionCreateDto ChoiceQuestion(string key = "storage", string options = "Garage,Street,Locked shed") => new()
    {
        Key = key,
        Label = "Where is it stored?",
        Type = QuestionType.Choice,
        IsRequired = true,
        Options = options,
        SortOrder = 2
    };

    private static RuleInputDto NumberRule(decimal from, decimal to, decimal multiplier = 1.5m) => new()
    {
        FromValue = from,
        ToValue = to,
        Multiplier = multiplier,
        Description = "Number rule"
    };

    private static RuleInputDto ChoiceRule(string match, decimal multiplier = 0.9m) => new()
    {
        MatchValue = match,
        Multiplier = multiplier,
        Description = "Choice rule"
    };

    // ---------- QUESTIONS ----------

    [Fact]
    public async Task CreateQuestion_NewKey_IsSaved()
    {
        var (service, _) = CreateService();

        var result = await service.CreateQuestionAsync(ProductCode, NumberQuestion());

        Assert.NotNull(result);
        Assert.Equal("riderAge", result!.Key);
        Assert.Equal(QuestionType.Number, result.Type);
        Assert.Single((await service.GetQuestionsAsync(ProductCode))!);
    }

    [Fact]
    public async Task CreateQuestion_SameKeyDifferentCase_ThrowsConflict()
    {
        var (service, _) = CreateService();
        await service.CreateQuestionAsync(ProductCode, NumberQuestion("riderAge"));

        await Assert.ThrowsAsync<ConflictException>(
            () => service.CreateQuestionAsync(ProductCode, NumberQuestion("RIDERAGE")));
    }

    [Fact]
    public async Task CreateQuestion_ChoiceWithOneOption_ThrowsBadRequest()
    {
        var (service, _) = CreateService();

        await Assert.ThrowsAsync<BadRequestException>(
            () => service.CreateQuestionAsync(ProductCode, ChoiceQuestion(options: "Yes")));
    }

    [Fact]
    public async Task CreateQuestion_ChoiceOptions_AreCleaned()
    {
        var (service, _) = CreateService();

        var result = await service.CreateQuestionAsync(ProductCode, ChoiceQuestion(options: "Yes, No,, yes"));

        Assert.Equal("Yes,No", result!.Options);
    }

    [Fact]
    public async Task CreateQuestion_NumberWithOptions_ThrowsBadRequest()
    {
        var (service, _) = CreateService();
        var input = NumberQuestion();
        input.Options = "A,B";

        await Assert.ThrowsAsync<BadRequestException>(() => service.CreateQuestionAsync(ProductCode, input));
    }

    [Fact]
    public async Task CreateQuestion_MinBiggerThanMax_ThrowsBadRequest()
    {
        var (service, _) = CreateService();
        var input = NumberQuestion();
        input.MinValue = 90;
        input.MaxValue = 10;

        await Assert.ThrowsAsync<BadRequestException>(() => service.CreateQuestionAsync(ProductCode, input));
    }

    [Fact]
    public async Task CreateQuestion_UnknownProduct_ReturnsNull()
    {
        var (service, _) = CreateService();

        var result = await service.CreateQuestionAsync("NOPE", NumberQuestion());

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateQuestion_RemovingOptionUsedByRule_ThrowsBadRequest()
    {
        var (service, _) = CreateService();
        await service.CreateQuestionAsync(ProductCode, ChoiceQuestion(options: "Garage,Street"));
        await service.AddRuleAsync(ProductCode, "storage", ChoiceRule("Garage"));

        var update = new QuestionUpdateDto
        {
            Label = "Where is it stored?",
            IsRequired = true,
            Options = "Street,Indoor",
            SortOrder = 2
        };

        await Assert.ThrowsAsync<BadRequestException>(
            () => service.UpdateQuestionAsync(ProductCode, "storage", update));
    }

    [Fact]
    public async Task UpdateQuestion_ChangeLabel_KeepsKeyAndType()
    {
        var (service, _) = CreateService();
        await service.CreateQuestionAsync(ProductCode, NumberQuestion());

        var update = new QuestionUpdateDto
        {
            Label = "Age of the rider",
            IsRequired = true,
            MinValue = 16,
            MaxValue = 90,
            SortOrder = 1
        };
        var result = await service.UpdateQuestionAsync(ProductCode, "riderAge", update);

        Assert.Equal("Age of the rider", result!.Label);
        Assert.Equal("riderAge", result.Key);
        Assert.Equal(QuestionType.Number, result.Type);
    }

    [Fact]
    public async Task DeleteQuestion_AlreadyAnswered_ThrowsConflict()
    {
        var (service, db) = CreateService();
        await service.CreateQuestionAsync(ProductCode, NumberQuestion());

        var product = db.Products.First(p => p.Code == ProductCode);
        db.Quotes.Add(new Quote
        {
            CustomerId = 1,
            ProductId = product.Id,
            Answers = "{\"riderAge\":\"20\"}",
            CoverageAmount = 10000,
            RequestedEffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30)
        });
        db.SaveChanges();

        await Assert.ThrowsAsync<ConflictException>(
            () => service.DeleteQuestionAsync(ProductCode, "riderAge"));
    }

    [Fact]
    public async Task DeleteQuestion_NotAnswered_RemovesQuestionAndItsRules()
    {
        var (service, db) = CreateService();
        await service.CreateQuestionAsync(ProductCode, NumberQuestion());
        await service.AddRuleAsync(ProductCode, "riderAge", NumberRule(16, 24));

        var removed = await service.DeleteQuestionAsync(ProductCode, "riderAge");

        Assert.True(removed);
        Assert.Empty((await service.GetQuestionsAsync(ProductCode))!);
        Assert.Empty(db.Set<RatingRules>());
    }

    // ---------- RULES ----------

    [Fact]
    public async Task AddRule_OverlappingRange_ThrowsConflict()
    {
        var (service, _) = CreateService();
        await service.CreateQuestionAsync(ProductCode, NumberQuestion());
        await service.AddRuleAsync(ProductCode, "riderAge", NumberRule(16, 24));

        await Assert.ThrowsAsync<ConflictException>(
            () => service.AddRuleAsync(ProductCode, "riderAge", NumberRule(20, 30)));
    }

    [Fact]
    public async Task AddRule_RangeTouchingAnotherAtOneNumber_ThrowsConflict()
    {
        var (service, _) = CreateService();
        await service.CreateQuestionAsync(ProductCode, NumberQuestion());
        await service.AddRuleAsync(ProductCode, "riderAge", NumberRule(16, 25));

        // Both ends are included, so 25 would match both rules
        await Assert.ThrowsAsync<ConflictException>(
            () => service.AddRuleAsync(ProductCode, "riderAge", NumberRule(25, 30)));
    }

    [Fact]
    public async Task AddRule_ChoiceAnswerAlreadyHasRule_ThrowsConflict()
    {
        var (service, _) = CreateService();
        await service.CreateQuestionAsync(ProductCode, ChoiceQuestion());
        await service.AddRuleAsync(ProductCode, "storage", ChoiceRule("Garage"));

        await Assert.ThrowsAsync<ConflictException>(
            () => service.AddRuleAsync(ProductCode, "storage", ChoiceRule("garage", 0.8m)));
    }

    [Fact]
    public async Task AddRule_ChoiceUnknownOption_ThrowsBadRequest()
    {
        var (service, _) = CreateService();
        await service.CreateQuestionAsync(ProductCode, ChoiceQuestion());

        await Assert.ThrowsAsync<BadRequestException>(
            () => service.AddRuleAsync(ProductCode, "storage", ChoiceRule("Roof")));
    }

    [Fact]
    public async Task AddRule_ChoiceMatch_IsSavedWithExactOptionSpelling()
    {
        var (service, _) = CreateService();
        await service.CreateQuestionAsync(ProductCode, ChoiceQuestion());

        var rule = await service.AddRuleAsync(ProductCode, "storage", ChoiceRule("garage"));

        Assert.Equal("Garage", rule!.MatchValue);
    }

    [Fact]
    public async Task UpdateRule_KeepingSameRange_DoesNotConflictWithItself()
    {
        var (service, _) = CreateService();
        await service.CreateQuestionAsync(ProductCode, NumberQuestion());
        var rule = await service.AddRuleAsync(ProductCode, "riderAge", NumberRule(16, 24, 1.5m));

        var updated = await service.UpdateRuleAsync(ProductCode, "riderAge", rule!.Id, NumberRule(16, 24, 1.6m));

        Assert.NotNull(updated);
        Assert.Equal(1.6m, updated!.Multiplier);
    }

    [Fact]
    public async Task DeleteRule_RemovesIt()
    {
        var (service, _) = CreateService();
        await service.CreateQuestionAsync(ProductCode, NumberQuestion());
        var rule = await service.AddRuleAsync(ProductCode, "riderAge", NumberRule(16, 24));

        var first = await service.DeleteRuleAsync(ProductCode, "riderAge", rule!.Id);
        var again = await service.DeleteRuleAsync(ProductCode, "riderAge", rule.Id);

        Assert.True(first);
        Assert.False(again);
    }
}