using InsuranceApi.Models;

namespace InsuranceApi.Dtos;

public class PolicyDto
{
    public int Id { get; set; }
    public string PolicyNumber { get; set; } = "";
    public int Segment { get; set; }
    public int Term { get; set; }
    public int CustomerId { get; set; }
    public int QuoteId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public decimal CoverageAmount { get; set; }
    public decimal Premium { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public DateOnly ExpirationDate { get; set; }
    public PolicyStatus Status { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime CreatedAt { get; set; }
}