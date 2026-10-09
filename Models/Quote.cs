namespace InsuranceApi.Models;

public class Quote
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public ProductType ProductType { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public decimal CoverageAmount { get; set; }
    public decimal Premium { get; set; }
    public DateOnly RequestedEffectiveDate { get; set; }
    public QuoteStatus Status { get; set; } = QuoteStatus.Quoted;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
}
