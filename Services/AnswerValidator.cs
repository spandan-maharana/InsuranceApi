using System.Globalization;
using System.Text.Json;
using InsuranceApi.Data;
using InsuranceApi.Dtos;
using InsuranceApi.Models;

namespace InsuranceApi.Services;

// Checks a customer's answers against a product's questions.
public static class AnswerValidator
{
    // Returns the answers as clean text, ready to save.
    // Throws BadRequestException with a clear message if anything is wrong.
    public static Dictionary<string, string> Validate(Product product, IDictionary<string, JsonElement>? input)
    {
        var clean = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Copy the input so the key lookup ignores upper/lower case
        var given = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        if (input != null)
        {
            foreach (var pair in input)
            {
                given[pair.Key] = pair.Value;
            }
        }

        // 1. Every answer must belong to one of the product's questions
        foreach (var key in given.Keys)
        {
            var known = product.Questions.Any(q => string.Equals(q.Key, key, StringComparison.OrdinalIgnoreCase));
            if (!known)
            {
                throw new BadRequestException($"'{key}' is not a question for {product.Name}.");
            }
        }

        // 2. Check each question in turn
        foreach (var question in product.Questions.OrderBy(q => q.SortOrder))
        {
            var hasAnswer = given.TryGetValue(question.Key, out var element) && !IsEmpty(element);

            if (!hasAnswer)
            {
                if (question.IsRequired)
                {
                    throw new BadRequestException($"'{question.Label}' is required.");
                }
                continue;
            }

            clean[question.Key] = question.Type == QuestionType.Number
                ? CheckNumber(question, element)
                : CheckChoice(question, element);
        }

        return clean;
    }

    private static bool IsEmpty(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined)
        {
            return true;
        }

        return element.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(element.GetString());
    }

    private static string CheckNumber(ProductQuestion question, JsonElement element)
    {
        decimal number;

        if (element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out var fromNumber))
        {
            number = fromNumber;
        }
        else if (element.ValueKind == JsonValueKind.String &&
                 decimal.TryParse(element.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var fromText))
        {
            number = fromText;
        }
        else
        {
            throw new BadRequestException($"'{question.Label}' must be a number.");
        }

        var tooLow = question.MinValue != null && number < question.MinValue;
        var tooHigh = question.MaxValue != null && number > question.MaxValue;
        if (tooLow || tooHigh)
        {
            var min = question.MinValue?.ToString("0.##", CultureInfo.InvariantCulture) ?? "no limit";
            var max = question.MaxValue?.ToString("0.##", CultureInfo.InvariantCulture) ?? "no limit";
            throw new BadRequestException($"'{question.Label}' must be between {min} and {max}.");
        }

        return number.ToString(CultureInfo.InvariantCulture);
    }

    private static string CheckChoice(ProductQuestion question, JsonElement element)
    {
        string? text = element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            _ => null
        };

        var options = SplitOptions(question.Options);

        // Use the proper spelling from the list, so rules always match
        var match = options.FirstOrDefault(o => string.Equals(o, text?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (match == null)
        {
            throw new BadRequestException($"'{question.Label}' must be one of: {string.Join(", ", options)}.");
        }

        return match;
    }

    // "Yes,No" becomes ["Yes", "No"]
    private static List<string> SplitOptions(string? options)
    {
        if (string.IsNullOrWhiteSpace(options))
        {
            return new List<string>();
        }

        return options
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }
}