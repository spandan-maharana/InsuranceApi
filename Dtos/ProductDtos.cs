using InsuranceApi.Models;
namespace InsuranceApi.Dtos;

public class ProductSummaryDto
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
}

public class CategoryDto
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public List<ProductSummaryDto> Products { get; set; } = new();
}

public class QuestionDto
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public QuestionType Type { get; set; }
    public bool IsRequired { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public List<string> Options { get; set; } = new();
}

public class ProductDtos
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string CategoryCode { get; set; } = "";
    public string CategoryName { get; set; } = "";

    // What the coverage amount means, e.g. "Trip cost"
    public string CoverageLabel { get; set; } = "";
    public decimal MinCoverage { get; set; }
    public decimal MaxCoverage { get; set; }

    public List<QuestionDto> Questions { get; set; } = new();

}
