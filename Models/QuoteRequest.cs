namespace InsuranceApi.Models;

public enum RequestStatus { New, Accepted, Declined}
public class QuoteRequest
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public decimal CoverageAmount { get; set; }
    public DateOnly RequestedEffectiveDate { get; set; }
    public string Answers { get; set; } = "{}";
    public RequestStatus Status { get; set; } = RequestStatus.New;
    public string? DeclineReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }

    public int? CustomerId { get; set; }
    public int? QuoteId { get; set; }
}
