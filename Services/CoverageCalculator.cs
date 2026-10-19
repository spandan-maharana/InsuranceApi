using System.Globalization;
using InsuranceApi.Dtos;
using InsuranceApi.Models;

namespace InsuranceApi.Services;

public static class CoverageCalculator
{
    // The product must be loaded together with its Questions and their CoverageRules
    public static CoverageResult Calculate(
        Product product,
        IReadOnlyDictionary<string, string> answers)
    {
        var result = new CoverageResult();

        // Start from the base coverage. If it was never set, start from the product minimum.
        var running = product.BaseCoverage > 0 ? product.BaseCoverage : product.MinCoverage;
        result.Steps.Add(new CoverageLine
        {
            Description = product.BaseCoverage > 0
                ? $"Starting {product.CoverageLabel.ToLowerInvariant()}: {running:N0}"
                : $"Starting at the product minimum: {running:N0}",
            Amount = Round(running)
        });

        // Apply the matching rule for each answered question, one after the other
        foreach (var question in product.Questions.OrderBy(q => q.SortOrder))
        {
            if (!answers.TryGetValue(question.Key, out var answer) || string.IsNullOrWhiteSpace(answer))
            {
                continue;
            }

            var rule = question.CoverageRules.FirstOrDefault(r => Matches(question, r, answer));
            if (rule == null)
            {
                continue;
            }

            running *= rule.Multiplier;
            result.Steps.Add(new CoverageLine
            {
                Description = rule.Description,
                Multiplier = rule.Multiplier,
                Amount = Round(running)
            });
        }

        // Keep the result inside the product's limits
        if (running < product.MinCoverage)
        {
            running = product.MinCoverage;
            result.LimitApplied = true;
            result.Steps.Add(new CoverageLine
            {
                Description = $"Raised to the product minimum: {running:N0}",
                Amount = Round(running)
            });
        }
        else if (product.MaxCoverage > 0 && running > product.MaxCoverage)
        {
            running = product.MaxCoverage;
            result.LimitApplied = true;
            result.Steps.Add(new CoverageLine
            {
                Description = $"Lowered to the product maximum: {running:N0}",
                Amount = Round(running)
            });
        }

        result.Coverage = Round(running);
        return result;
    }

    private static bool Matches(ProductQuestion question, CoverageRule rule, string answer)
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

    // Money is kept to the cent (this is not a visible rounding step)
    private static decimal Round(decimal value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}