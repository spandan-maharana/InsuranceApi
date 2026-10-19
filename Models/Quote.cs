using System.ComponentModel.DataAnnotations;

namespace InsuranceApi.Models;

public class Quote
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public string Answers { get; set; } = "{}";
    public decimal CoverageAmount { get; set; }
    public decimal? Premium { get; set; } //Premium can be empty
    public string? PremiumBreakdown { get; set; }
    public DateOnly RequestedEffectiveDate { get; set; }
    public QuoteStatus Status { get; set; } = QuoteStatus.Draft; //Quotes default to draft status
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; } //Expiry time can be empty
    [MaxLength(100)]
    public string? CreatedBy { get; set; }
}
