namespace InsuranceApi.Models;

public class RatingRules
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public ProductQuestion? Question { get; set; }

    // For Choice questions: the answer this rule matches, e.g. "Metal"
    public string? MatchValue { get; set; }

    // For Number questions: the range this rule matches (both ends included)
    public decimal? FromValue { get; set; }
    public decimal? ToValue { get; set; }

    // The price is multiplied by this when the rule matches
    public decimal Multiplier { get; set; } = 1m;

    // Shown in the premium breakdown, e.g. "Young driver"
    public string Description { get; set; } = string.Empty;
}