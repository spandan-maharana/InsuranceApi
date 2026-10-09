namespace InsuranceApi.Models;

public class Policy
{
    public int Id { get; set; }
    public string PolicyNumber { get; set; } = "";
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int QuoteId { get; set; }
    public Quote? Quote { get; set; }
    public ProductType ProductType { get; set; }
    public decimal CoverageAmount { get; set; }
    public decimal Premium { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public DateOnly ExpirationDate { get; set; }
    public PolicyStatus Status { get; set; } = PolicyStatus.Active;
    public DateTime? CancelledAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

