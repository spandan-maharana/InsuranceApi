using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using InsuranceApi.Models;

namespace InsuranceApi.Dtos;

//Data Sent By Client
public class QuoteInputDto
{
    [Range(1, int.MaxValue)]
    public int customerId { get; set; }
    [Required]
    public string? ProductCode { get; set; }
    [Range(1000, 10000000)]
    public decimal CoverageAmount { get; set; }
    public DateOnly RequestedEffectiveDate { get; set; }
    public Dictionary<string, JsonElement>? Answers { get; set; }
}

//Data Sent to Client
public class QuoteDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public Dictionary<string, string> Answers { get; set; } = new();
    public decimal CoverageAmount { get; set; }
    public decimal? Premium { get; set; } //Acceptable to null values
    public PremiumResult? PremiumBreakdown { get; set; }
    public DateOnly RequestedEffectiveDate { get; set; }
    public QuoteStatus Status { get; set; }
    public string? CreatedBy { get; set; } //The agent who owns this quote
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; } //Acceptable to null values
}

public class QuoteHistoryDto
{
    public int Id { get; set; }
    public int QuoteId { get; set; }
    public string Action { get; set; } = "";
    public string FromStatus { get; set; } = "";
    public string ToStatus { get; set; } = "";
    public DateTime OccuredAt { get; set; }
    public string? PerformedBy { get; set; }
}