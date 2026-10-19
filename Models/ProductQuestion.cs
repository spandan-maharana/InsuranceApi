namespace InsuranceApi.Models;

public enum QuestionType { Number, Choice }

public class ProductQuestion
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }

    // Name used in the answers, e.g. driverAge
    public string Key { get; set; } = string.Empty;

    // Text shown to the user, e.g. "Driver age"
    public string Label { get; set; } = string.Empty;

    public QuestionType Type { get; set; }
    public bool IsRequired { get; set; } = true;

    // For Number questions: allowed range (optional)
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }

    // For Choice questions: allowed answers, separated by commas, e.g. "Yes,No"
    public string? Options { get; set; }

    public int SortOrder { get; set; }

    public List<RatingRules> Rules { get; set; } = new();
    public List<CoverageRule> CoverageRules { get; set; } = new();
}