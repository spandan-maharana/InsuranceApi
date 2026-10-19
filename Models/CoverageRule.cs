using System.ComponentModel.DataAnnotations.Schema;

namespace InsuranceApi.Models;

// Works like RatingRules, but for the recommended coverage.
// When the rule matches an answer, the coverage is multiplied by Multiplier.
[Table("CoverageRules")]
public class CoverageRule
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public ProductQuestion? Question { get; set; }

    // For Choice questions: the answer this rule matches, e.g. "SUV"
    public string? MatchValue { get; set; }

    // For Number questions: the range this rule matches (both ends included)
    public decimal? FromValue { get; set; }
    public decimal? ToValue { get; set; }

    // The coverage is multiplied by this when the rule matches
    public decimal Multiplier { get; set; } = 1m;

    // Shown in the coverage breakdown, e.g. "Young driver: starter limit"
    public string Description { get; set; } = string.Empty;
}