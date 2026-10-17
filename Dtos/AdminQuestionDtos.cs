using System.ComponentModel.DataAnnotations;
using InsuranceApi.Models;

namespace InsuranceApi.Dtos;

// What the SuperUser sends to ADD a question
public class QuestionCreateDto
{
    // Name used in the answers, e.g. driverAge. Cannot be changed later.
    [Required]
    [StringLength(40, MinimumLength = 2)]
    [RegularExpression("^[A-Za-z][A-Za-z0-9_]*$",
        ErrorMessage = "Key must start with a letter and use only letters, numbers and underscores.")]
    public string Key { get; set; } = string.Empty;

    // Text shown to the customer, e.g. "Driver age"
    [Required]
    [StringLength(100)]
    public string Label { get; set; } = string.Empty;

    // Number or Choice. Cannot be changed later.
    public QuestionType Type { get; set; }

    public bool IsRequired { get; set; } = true;

    // Number questions only
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }

    // Choice questions only, separated by commas, e.g. "Yes,No"
    [StringLength(500)]
    public string? Options { get; set; }

    public int SortOrder { get; set; }
}

// What the SuperUser sends to EDIT a question (key and type are fixed)
public class QuestionUpdateDto
{
    [Required]
    [StringLength(100)]
    public string Label { get; set; } = string.Empty;

    public bool IsRequired { get; set; } = true;

    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }

    [StringLength(500)]
    public string? Options { get; set; }

    public int SortOrder { get; set; }
}

// What the SuperUser sends to ADD or EDIT a rating rule
public class RuleInputDto
{
    // Choice questions: the answer this rule matches, e.g. "Yes"
    [StringLength(100)]
    public string? MatchValue { get; set; }

    // Number questions: the range this rule matches (both ends included)
    public decimal? FromValue { get; set; }
    public decimal? ToValue { get; set; }

    // The price is multiplied by this when the rule matches
    [Range(0.01, 100)]
    public decimal Multiplier { get; set; } = 1m;

    // Shown in the premium breakdown, e.g. "Young driver"
    [Required]
    [StringLength(100)]
    public string Description { get; set; } = string.Empty;
}

// What the API sends back for a rule
public class AdminRuleDto
{
    public int Id { get; set; }
    public string? MatchValue { get; set; }
    public decimal? FromValue { get; set; }
    public decimal? ToValue { get; set; }
    public decimal Multiplier { get; set; }
    public string Description { get; set; } = string.Empty;
}

// What the API sends back for a question, with its rules inside
public class AdminQuestionDtos
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public bool IsRequired { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public string? Options { get; set; }
    public int SortOrder { get; set; }
    public List<AdminRuleDto> Rules { get; set; } = new();
}