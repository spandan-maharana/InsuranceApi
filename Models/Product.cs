namespace InsuranceApi.Models;

public class Product
{
    public int Id { get; set; }

    public int CategoryId { get; set; }
    public ProductCategory? Category { get; set; }

    // Short unique code used in requests, e.g. AUTO, HOME
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // What the coverage amount means for this product, e.g. "Coverage amount", "Trip cost"
    public string CoverageLabel { get; set; } = "Coverage amount";

    // Annual premium rate for every $1,000 of coverage
    public decimal RatePerThousand { get; set; }

    // No policy costs less than this (annual)
    public decimal MinimumPremium { get; set; }

    // Flat fee added to every policy (annual)
    public decimal PolicyFee { get; set; }

    public decimal MinCoverage { get; set; }
    public decimal MaxCoverage { get; set; }

    public int SortOrder { get; set; }

    // Inactive products cannot get new quotes
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<ProductQuestion> Questions { get; set; } = new();
}