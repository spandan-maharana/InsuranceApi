using System.Globalization;
using InsuranceApi.Dtos;
using InsuranceApi.Models;

namespace InsuranceApi.Services;

public static class RatingCalculator
{
    public static PremiumResult Calculate(
        Product product,
        decimal coverageAmount,
        IReadOnlyDictionary<string, string> answers)
    {
        var result = new PremiumResult();

        // Base price from the coverage amount
        var running = coverageAmount / 1000m * product.RatePerThousand;
        result.Steps.Add(new PremiumStep
        {
            Description = $"Base: {product.RatePerThousand:0.####} per $1,000 of coverage ({coverageAmount:N0})",
            Amount = Round(running)
        });

        foreach (var question in product.Questions.OrderBy(q => q.SortOrder))
        {
            if (!answers.TryGetValue(question.Key, out var answer) || string.IsNullOrWhiteSpace(answer))
            {
                continue;
            }

            var rule = question.Rules.FirstOrDefault(r => Matches(question, r, answer));
            if (rule == null)
            {
                continue;
            }

            running *= rule.Multiplier;
            result.Steps.Add(new PremiumStep
            {
                Description = rule.Description,
                Multiplier = rule.Multiplier,
                Amount = Round(running)
            });
        }

        running += product.PolicyFee;
        result.Steps.Add(new PremiumStep
        {
            Description = $"Policy fee: +{product.PolicyFee:N2}",
            Amount = Round(running)
        });

        if (running < product.MinimumPremium)
        {
            running = product.MinimumPremium;
            result.MinimumApplied = true;
            result.Steps.Add(new PremiumStep
            {
                Description = $"Minimum premium applied: {product.MinimumPremium:N2}",
                Amount = Round(running)
            });
        }

        result.Premium = Round(running);
        return result;
    }

    private static bool Matches(ProductQuestion question, RatingRules rule, string answer)
    {
        // Choice question: the answer must equal the rule's value
        if (question.Type == QuestionType.Choice)
        {
            return string.Equals(rule.MatchValue, answer.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        // Number question: the number must fall in the rule's range (both ends included)
        if (!decimal.TryParse(answer, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
        {
            return false;
        }

        var aboveStart = rule.FromValue == null || number >= rule.FromValue;
        var belowEnd = rule.ToValue == null || number <= rule.ToValue;
        return aboveStart && belowEnd;
    }

    private static decimal Round(decimal value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
