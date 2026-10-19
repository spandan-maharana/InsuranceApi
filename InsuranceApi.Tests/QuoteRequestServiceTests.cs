using System.Text.Json;
using InsuranceApi.Data;
using InsuranceApi.Dtos;
using InsuranceApi.Models;
using InsuranceApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace InsuranceApi.Tests;

public class QuoteRequestServiceTests
{
    // Every test gets its own fresh, empty database, filled with the real product catalogue
    private static InsuranceDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<InsuranceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new InsuranceDbContext(options);
        ProductCatalogLoader.SeedAsync(db).GetAwaiter().GetResult();
        return db;
    }

    private static QuoteRequestService NewService(InsuranceDbContext db)
    {
        return new QuoteRequestService(db, NullLogger<QuoteRequestService>.Instance);
    }

    // A good Auto request. The engine recommends 125,000 for it.
    // Tests change one thing at a time from this.
    private static RequestInputDto GoodInput(string email = "maya@example.com")
    {
        return new RequestInputDto
        {
            FirstName = "Maya",
            LastName = "Rivera",
            Email = email,
            PhoneNumber = "603-555-0142",
            ProductCode = "AUTO",
            RequestedEffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(1),
            Answers = Answers(("driverAge", 40), ("accidentsLast3Years", 0), ("vehicleType", "Sedan"))
        };
    }

    private static Dictionary<string, JsonElement> Answers(params (string Key, object Value)[] pairs)
    {
        return pairs.ToDictionary(p => p.Key, p => JsonSerializer.SerializeToElement(p.Value));
    }

    // ---------- Submit ----------

    [Fact]
    public async Task Submit_ValidRequest_SavesRequestAsNew()
    {
        using var db = NewDb();
        var service = NewService(db);

        var receipt = await service.SubmitAsync(GoodInput());

        var saved = await db.QuoteRequests.SingleAsync();
        Assert.Equal(receipt.Id, saved.Id);
        Assert.Equal(RequestStatus.New, saved.Status);
        Assert.Null(saved.CustomerId);
        Assert.Null(saved.QuoteId);
        Assert.Contains("\"driverAge\":\"40\"", saved.Answers);
    }

    [Fact]
    public async Task Submit_ValidRequest_SavesRecommendedCoverageAndBreakdown()
    {
        using var db = NewDb();
        var service = NewService(db);

        await service.SubmitAsync(GoodInput());

        // 100,000 base x 1.25 (adult driver) x 1.00 (no accidents) x 1.00 (sedan) = 125,000
        var saved = await db.QuoteRequests.SingleAsync();
        Assert.Equal(125000m, saved.CoverageAmount);
        Assert.False(string.IsNullOrWhiteSpace(saved.CoverageBreakdown));
    }

    [Fact]
    public async Task Submit_RiskierProfile_GetsHigherRecommendedCoverage()
    {
        using var db = NewDb();
        var service = NewService(db);
        var input = GoodInput();
        input.Answers = Answers(("driverAge", 30), ("accidentsLast3Years", 2), ("vehicleType", "SUV"));

        await service.SubmitAsync(input);

        // 100,000 x 1.25 x 1.50 x 1.25 = 234,375, rounded to the nearest 5,000 = 235,000
        var saved = await db.QuoteRequests.SingleAsync();
        Assert.Equal(234375m, saved.CoverageAmount);
    }

    [Fact]
    public async Task Submit_ValidRequest_DoesNotCreateCustomerOrQuote()
    {
        using var db = NewDb();
        var service = NewService(db);

        await service.SubmitAsync(GoodInput());

        Assert.Equal(0, await db.Customers.CountAsync());
        Assert.Equal(0, await db.Quotes.CountAsync());
    }

    [Fact]
    public async Task Submit_PastDate_ThrowsAndSavesNothing()
    {
        using var db = NewDb();
        var service = NewService(db);
        var input = GoodInput();
        input.RequestedEffectiveDate = new DateOnly(2020, 1, 1);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => service.SubmitAsync(input));

        Assert.Contains("past", ex.Message);
        Assert.Equal(0, await db.QuoteRequests.CountAsync());
    }

    [Fact]
    public async Task Submit_UnknownProduct_Throws()
    {
        using var db = NewDb();
        var service = NewService(db);
        var input = GoodInput();
        input.ProductCode = "NOPE";

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => service.SubmitAsync(input));

        Assert.Equal("Unknown or unavailable product.", ex.Message);
    }

    [Fact]
    public async Task Submit_InactiveProduct_Throws()
    {
        using var db = NewDb();
        var auto = await db.Products.FirstAsync(p => p.Code == "AUTO");
        auto.IsActive = false;
        await db.SaveChangesAsync();
        var service = NewService(db);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => service.SubmitAsync(GoodInput()));

        Assert.Equal("Unknown or unavailable product.", ex.Message);
    }

    [Fact]
    public async Task Submit_MissingRequiredAnswer_Throws()
    {
        using var db = NewDb();
        var service = NewService(db);
        var input = GoodInput();
        input.Answers = Answers(("accidentsLast3Years", 0), ("vehicleType", "Sedan"));

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => service.SubmitAsync(input));

        Assert.Contains("required", ex.Message);
    }

    [Fact]
    public async Task Submit_AnswerForUnknownQuestion_Throws()
    {
        using var db = NewDb();
        var service = NewService(db);
        var input = GoodInput();
        input.Answers = Answers(("driverAge", 40), ("accidentsLast3Years", 0),
            ("vehicleType", "Sedan"), ("favouriteColour", "Blue"));

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => service.SubmitAsync(input));

        Assert.Contains("favouriteColour", ex.Message);
    }

    // ---------- List and Get ----------

    [Fact]
    public async Task List_ReturnsOldestFirst()
    {
        using var db = NewDb();
        var service = NewService(db);
        var first = await service.SubmitAsync(GoodInput("first@example.com"));
        var second = await service.SubmitAsync(GoodInput("second@example.com"));

        // Make the second one older, so we know the list sorts by date
        (await db.QuoteRequests.FindAsync(first.Id))!.CreatedAt = DateTime.UtcNow.AddHours(-1);
        (await db.QuoteRequests.FindAsync(second.Id))!.CreatedAt = DateTime.UtcNow.AddHours(-2);
        await db.SaveChangesAsync();

        var list = await service.ListAsync(null);

        Assert.Equal(2, list.Count);
        Assert.Equal(second.Id, list[0].Id);
        Assert.Equal(first.Id, list[1].Id);
    }

    [Fact]
    public async Task List_FilterByStatus_ReturnsOnlyThatStatus()
    {
        using var db = NewDb();
        var service = NewService(db);
        var a = await service.SubmitAsync(GoodInput("a@example.com"));
        await service.SubmitAsync(GoodInput("b@example.com"));
        await service.AcceptAsync(a.Id);

        var newOnes = await service.ListAsync(RequestStatus.New);
        var accepted = await service.ListAsync(RequestStatus.Accepted);

        Assert.Single(newOnes);
        Assert.Single(accepted);
        Assert.Equal(a.Id, accepted[0].Id);
    }

    [Fact]
    public async Task Get_ShowsProductAndAnswers()
    {
        using var db = NewDb();
        var service = NewService(db);
        var receipt = await service.SubmitAsync(GoodInput());

        var dto = await service.GetAsync(receipt.Id);

        Assert.NotNull(dto);
        Assert.Equal("AUTO", dto!.ProductCode);
        Assert.Equal("40", dto.Answers["driverAge"]);
        Assert.Equal("Sedan", dto.Answers["vehicleType"]);
    }

    [Fact]
    public async Task Get_ShowsCoverageLimitsAndBreakdown()
    {
        using var db = NewDb();
        var service = NewService(db);
        var receipt = await service.SubmitAsync(GoodInput());

        var dto = await service.GetAsync(receipt.Id);

        Assert.Equal(125000m, dto!.CoverageAmount);
        Assert.Equal(10000m, dto.MinCoverage);
        Assert.Equal(500000m, dto.MaxCoverage);
        Assert.Equal("Liability coverage", dto.CoverageLabel);
        Assert.NotNull(dto.CoverageBreakdown);
        Assert.Equal(125000m, dto.CoverageBreakdown!.Coverage);
        Assert.NotEmpty(dto.CoverageBreakdown.Steps);
    }

    [Fact]
    public async Task Get_MissingRequest_ReturnsNull()
    {
        using var db = NewDb();
        var service = NewService(db);

        Assert.Null(await service.GetAsync(99999));
    }

    // ---------- Accept ----------

    [Fact]
    public async Task Accept_NewPerson_CreatesCustomerAndDraftQuote()
    {
        using var db = NewDb();
        var service = NewService(db);
        var receipt = await service.SubmitAsync(GoodInput("new.person@example.com"));

        var result = await service.AcceptAsync(receipt.Id);

        Assert.NotNull(result);
        Assert.Equal(RequestStatus.Accepted, result!.Status);
        Assert.NotNull(result.CustomerId);
        Assert.NotNull(result.QuoteId);
        Assert.NotNull(result.ReviewedAt);

        var customer = await db.Customers.SingleAsync();
        Assert.Equal("new.person@example.com", customer.Email);
        Assert.Equal("Maya", customer.FirstName);

        var quote = await db.Quotes.SingleAsync();
        Assert.Equal(customer.Id, quote.CustomerId);
        Assert.Equal(QuoteStatus.Draft, quote.Status);
        Assert.Null(quote.Premium);
        // With no agent override, the quote uses the engine's recommended coverage
        Assert.Equal(125000m, quote.CoverageAmount);
        Assert.Contains("\"vehicleType\":\"Sedan\"", quote.Answers);
    }

    [Fact]
    public async Task Accept_WithAgentOverride_QuoteUsesAgentAmount()
    {
        using var db = NewDb();
        var service = NewService(db);
        var receipt = await service.SubmitAsync(GoodInput());

        await service.AcceptAsync(receipt.Id, new AcceptRequestDto { CoverageAmount = 150000m });

        var quote = await db.Quotes.SingleAsync();
        Assert.Equal(150000m, quote.CoverageAmount);
    }

    [Fact]
    public async Task Accept_WithAgentOverride_RequestStillShowsRecommendedAmount()
    {
        using var db = NewDb();
        var service = NewService(db);
        var receipt = await service.SubmitAsync(GoodInput());

        var result = await service.AcceptAsync(receipt.Id, new AcceptRequestDto { CoverageAmount = 150000m });

        // The inbox keeps showing what the engine recommended
        Assert.Equal(125000m, result!.CoverageAmount);
    }

    [Fact]
    public async Task Accept_OverrideAboveProductMaximum_ThrowsAndRequestStaysNew()
    {
        using var db = NewDb();
        var service = NewService(db);
        var receipt = await service.SubmitAsync(GoodInput());

        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            service.AcceptAsync(receipt.Id, new AcceptRequestDto { CoverageAmount = 900000m }));

        Assert.Contains("must be between", ex.Message);
        Assert.Equal(RequestStatus.New, (await db.QuoteRequests.SingleAsync()).Status);
        Assert.Equal(0, await db.Customers.CountAsync());
        Assert.Equal(0, await db.Quotes.CountAsync());
    }

    [Fact]
    public async Task Accept_OverrideBelowProductMinimum_Throws()
    {
        using var db = NewDb();
        var service = NewService(db);
        var receipt = await service.SubmitAsync(GoodInput());

        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            service.AcceptAsync(receipt.Id, new AcceptRequestDto { CoverageAmount = 5000m }));

        Assert.Contains("must be between", ex.Message);
        Assert.Equal(0, await db.Quotes.CountAsync());
    }

    [Fact]
    public async Task Accept_EmailAlreadyACustomer_ReusesThatCustomer()
    {
        using var db = NewDb();
        var existing = new Customer
        {
            FirstName = "Maya",
            LastName = "Rivera",
            Email = "maya@example.com",
            PhoneNumber = "603-555-0100",
            CreatedAt = DateTime.UtcNow
        };
        db.Customers.Add(existing);
        await db.SaveChangesAsync();
        var service = NewService(db);
        var receipt = await service.SubmitAsync(GoodInput("maya@example.com"));

        var result = await service.AcceptAsync(receipt.Id);

        Assert.Equal(existing.Id, result!.CustomerId);
        Assert.Equal(1, await db.Customers.CountAsync());
        Assert.Equal(1, await db.Quotes.CountAsync());
    }

    [Fact]
    public async Task Accept_TwoRequestsSameEmail_ShareOneCustomerWithTwoQuotes()
    {
        using var db = NewDb();
        var service = NewService(db);
        var first = await service.SubmitAsync(GoodInput("same@example.com"));
        var second = await service.SubmitAsync(GoodInput("same@example.com"));

        var a = await service.AcceptAsync(first.Id);
        var b = await service.AcceptAsync(second.Id);

        Assert.Equal(a!.CustomerId, b!.CustomerId);
        Assert.NotEqual(a.QuoteId, b.QuoteId);
        Assert.Equal(1, await db.Customers.CountAsync());
        Assert.Equal(2, await db.Quotes.CountAsync());
    }

    [Fact]
    public async Task Accept_Twice_ThrowsConflictAndCreatesNothingExtra()
    {
        using var db = NewDb();
        var service = NewService(db);
        var receipt = await service.SubmitAsync(GoodInput());
        await service.AcceptAsync(receipt.Id);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => service.AcceptAsync(receipt.Id));

        Assert.Contains("already been accepted", ex.Message);
        Assert.Equal(1, await db.Customers.CountAsync());
        Assert.Equal(1, await db.Quotes.CountAsync());
    }

    [Fact]
    public async Task Accept_DeclinedRequest_ThrowsConflict()
    {
        using var db = NewDb();
        var service = NewService(db);
        var receipt = await service.SubmitAsync(GoodInput());
        await service.DeclineAsync(receipt.Id, new DeclineRequestDto { Reason = "Not a fit" });

        var ex = await Assert.ThrowsAsync<ConflictException>(() => service.AcceptAsync(receipt.Id));

        Assert.Contains("already been declined", ex.Message);
    }

    [Fact]
    public async Task Accept_MissingRequest_ReturnsNull()
    {
        using var db = NewDb();
        var service = NewService(db);

        Assert.Null(await service.AcceptAsync(99999));
    }

    // ---------- Decline ----------

    [Fact]
    public async Task Decline_SavesReasonAndCreatesNothing()
    {
        using var db = NewDb();
        var service = NewService(db);
        var receipt = await service.SubmitAsync(GoodInput());

        var result = await service.DeclineAsync(receipt.Id,
            new DeclineRequestDto { Reason = "  Risk is outside our appetite.  " });

        Assert.Equal(RequestStatus.Declined, result!.Status);
        Assert.Equal("Risk is outside our appetite.", result.DeclineReason);
        Assert.NotNull(result.ReviewedAt);
        Assert.Null(result.CustomerId);
        Assert.Null(result.QuoteId);
        Assert.Equal(0, await db.Customers.CountAsync());
        Assert.Equal(0, await db.Quotes.CountAsync());
    }

    [Fact]
    public async Task Decline_AlreadyAccepted_ThrowsConflict()
    {
        using var db = NewDb();
        var service = NewService(db);
        var receipt = await service.SubmitAsync(GoodInput());
        await service.AcceptAsync(receipt.Id);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            service.DeclineAsync(receipt.Id, new DeclineRequestDto { Reason = "Changed my mind" }));

        Assert.Contains("already been accepted", ex.Message);
    }

    [Fact]
    public async Task Decline_MissingRequest_ReturnsNull()
    {
        using var db = NewDb();
        var service = NewService(db);

        Assert.Null(await service.DeclineAsync(99999, new DeclineRequestDto { Reason = "Whatever" }));
    }
}