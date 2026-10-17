using InsuranceApi.Data;
using InsuranceApi.Dtos;
using InsuranceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace InsuranceApi.Services;

public class AdminQuestionService : IAdminQuestionService
{
    private readonly InsuranceDbContext _db;

    public AdminQuestionService(InsuranceDbContext db)
    {
        _db = db;
    }


    public async Task<List<AdminQuestionDtos>?> GetQuestionsAsync(string productCode)
    {
        var product = await LoadProductAsync(productCode);
        if (product == null) return null;

        return product.Questions
            .OrderBy(q => q.SortOrder).ThenBy(q => q.Id)
            .Select(ToDto)
            .ToList();
    }

    public async Task<AdminQuestionDtos?> CreateQuestionAsync(string productCode, QuestionCreateDto input)
    {
        var product = await LoadProductAsync(productCode);
        if (product == null) return null;

        var key = input.Key.Trim();
        if (FindQuestion(product, key) != null)
            throw new ConflictException($"A question with the key '{key}' already exists on this product.");

        var options = CheckQuestionShape(input.Type, input.MinValue, input.MaxValue, input.Options);

        var question = new ProductQuestion
        {
            Key = key,
            Label = input.Label.Trim(),
            Type = input.Type,
            IsRequired = input.IsRequired,
            MinValue = input.MinValue,
            MaxValue = input.MaxValue,
            Options = options,
            SortOrder = input.SortOrder
        };

        product.Questions.Add(question);
        product.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return ToDto(question);
    }

    public async Task<AdminQuestionDtos?> UpdateQuestionAsync(string productCode, string key, QuestionUpdateDto input)
    {
        var product = await LoadProductAsync(productCode);
        if (product == null) return null;

        var question = FindQuestion(product, key);
        if (question == null) return null;

        var options = CheckQuestionShape(question.Type, input.MinValue, input.MaxValue, input.Options);

        // Existing rules must still match one of the (new) options
        if (question.Type == QuestionType.Choice)
        {
            var newOptions = SplitOptions(options);
            foreach (var rule in question.Rules)
            {
                if (!newOptions.Contains(rule.MatchValue ?? string.Empty))
                    throw new BadRequestException(
                        $"The rule '{rule.Description}' uses the answer '{rule.MatchValue}', " +
                        "which would no longer be an option. Change or remove that rule first.");
            }
        }

        question.Label = input.Label.Trim();
        question.IsRequired = input.IsRequired;
        question.MinValue = input.MinValue;
        question.MaxValue = input.MaxValue;
        question.Options = options;
        question.SortOrder = input.SortOrder;
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ToDto(question);
    }

    public async Task<bool> DeleteQuestionAsync(string productCode, string key)
    {
        var product = await LoadProductAsync(productCode);
        if (product == null) return false;

        var question = FindQuestion(product, key);
        if (question == null) return false;

        // Answers are saved as JSON like {"driverAge":"40"}, so look for "key":
        var pattern = "\"" + question.Key + "\":";
        var alreadyAnswered = await _db.Set<Quote>()
            .AnyAsync(q => q.ProductId == product.Id && q.Answers.Contains(pattern));

        if (alreadyAnswered)
            throw new ConflictException(
                $"The question '{question.Key}' has already been answered on existing quotes, " +
                "so it cannot be removed. Make it not required instead.");

        _db.RemoveRange(question.Rules);
        _db.Remove(question);
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }


    public async Task<AdminRuleDto?> AddRuleAsync(string productCode, string key, RuleInputDto input)
    {
        var product = await LoadProductAsync(productCode);
        if (product == null) return null;

        var question = FindQuestion(product, key);
        if (question == null) return null;

        var rule = BuildRule(question, input, ignoreRuleId: null);
        question.Rules.Add(rule);
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ToRuleDto(rule);
    }

    public async Task<AdminRuleDto?> UpdateRuleAsync(string productCode, string key, int ruleId, RuleInputDto input)
    {
        var product = await LoadProductAsync(productCode);
        if (product == null) return null;

        var question = FindQuestion(product, key);
        if (question == null) return null;

        var existing = question.Rules.FirstOrDefault(r => r.Id == ruleId);
        if (existing == null) return null;

        var checkedRule = BuildRule(question, input, ignoreRuleId: ruleId);

        existing.MatchValue = checkedRule.MatchValue;
        existing.FromValue = checkedRule.FromValue;
        existing.ToValue = checkedRule.ToValue;
        existing.Multiplier = checkedRule.Multiplier;
        existing.Description = checkedRule.Description;
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ToRuleDto(existing);
    }

    public async Task<bool> DeleteRuleAsync(string productCode, string key, int ruleId)
    {
        var product = await LoadProductAsync(productCode);
        if (product == null) return false;

        var question = FindQuestion(product, key);
        if (question == null) return false;

        var rule = question.Rules.FirstOrDefault(r => r.Id == ruleId);
        if (rule == null) return false;

        _db.Remove(rule);
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }


    private async Task<Product?> LoadProductAsync(string productCode)
    {
        var code = (productCode ?? string.Empty).Trim().ToUpper();
        return await _db.Set<Product>()
            .Include(p => p.Questions)
            .ThenInclude(q => q.Rules)
            .FirstOrDefaultAsync(p => p.Code == code);
    }

    private static ProductQuestion? FindQuestion(Product product, string key)
    {
        var wanted = (key ?? string.Empty).Trim();
        return product.Questions.FirstOrDefault(q =>
            string.Equals(q.Key, wanted, StringComparison.OrdinalIgnoreCase));
    }

    private static List<string> SplitOptions(string? options)
    {
        return (options ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? CheckQuestionShape(QuestionType type, decimal? min, decimal? max, string? options)
    {
        if (!Enum.IsDefined(type))
            throw new BadRequestException("Type must be Number or Choice.");

        if (type == QuestionType.Number)
        {
            if (!string.IsNullOrWhiteSpace(options))
                throw new BadRequestException("A Number question cannot have options.");

            if (min.HasValue && max.HasValue && min.Value > max.Value)
                throw new BadRequestException("MinValue cannot be bigger than MaxValue.");

            return null;
        }

        if (min.HasValue || max.HasValue)
            throw new BadRequestException("A Choice question cannot have MinValue or MaxValue.");

        var list = SplitOptions(options);
        if (list.Count < 2)
            throw new BadRequestException("A Choice question needs at least 2 different options.");

        return string.Join(",", list);
    }

    private static RatingRules BuildRule(ProductQuestion question, RuleInputDto input, int? ignoreRuleId)
    {
        var others = question.Rules.Where(r => r.Id != (ignoreRuleId ?? 0)).ToList();

        var rule = new RatingRules
        {
            Multiplier = input.Multiplier,
            Description = input.Description.Trim()
        };

        if (question.Type == QuestionType.Choice)
        {
            if (string.IsNullOrWhiteSpace(input.MatchValue))
                throw new BadRequestException("A rule on a Choice question needs a matchValue.");

            if (input.FromValue.HasValue || input.ToValue.HasValue)
                throw new BadRequestException("A rule on a Choice question cannot have fromValue or toValue.");

            var wanted = input.MatchValue.Trim();
            var option = SplitOptions(question.Options)
                .FirstOrDefault(o => string.Equals(o, wanted, StringComparison.OrdinalIgnoreCase));

            if (option == null)
                throw new BadRequestException($"'{wanted}' is not one of the options for this question.");

            if (others.Any(r => r.MatchValue == option))
                throw new ConflictException($"A rule for the answer '{option}' already exists on this question.");

            rule.MatchValue = option;
            return rule;
        }

        if (!string.IsNullOrWhiteSpace(input.MatchValue))
            throw new BadRequestException("A rule on a Number question cannot have a matchValue.");

        if (!input.FromValue.HasValue || !input.ToValue.HasValue)
            throw new BadRequestException("A rule on a Number question needs both fromValue and toValue.");

        var from = input.FromValue.Value;
        var to = input.ToValue.Value;

        if (from > to)
            throw new BadRequestException("fromValue cannot be bigger than toValue.");

        foreach (var other in others)
        {
            var otherFrom = other.FromValue ?? decimal.MinValue;
            var otherTo = other.ToValue ?? decimal.MaxValue;

            if (from <= otherTo && otherFrom <= to)
                throw new ConflictException($"This range overlaps the existing rule '{other.Description}'.");
        }

        rule.FromValue = from;
        rule.ToValue = to;
        return rule;
    }

    private static AdminRuleDto ToRuleDto(RatingRules r) => new()
    {
        Id = r.Id,
        MatchValue = r.MatchValue,
        FromValue = r.FromValue,
        ToValue = r.ToValue,
        Multiplier = r.Multiplier,
        Description = r.Description
    };

    private static AdminQuestionDtos ToDto(ProductQuestion q) => new()
    {
        Id = q.Id,
        Key = q.Key,
        Label = q.Label,
        Type = q.Type,
        IsRequired = q.IsRequired,
        MinValue = q.MinValue,
        MaxValue = q.MaxValue,
        Options = q.Options,
        SortOrder = q.SortOrder,
        Rules = q.Rules.OrderBy(r => r.Id).Select(ToRuleDto).ToList()
    };
}