using System.ComponentModel.DataAnnotations;
using InsuranceApi.Models;

namespace InsuranceApi.Dtos;

//Data Sent By Client
public class QuoteInputDto
{
    [Range(1, int.MaxValue)]
    public int customerId { get; set; }

    [Required]
    public ProductType? ProductType { get; set; }
    
    [Required]
    public RiskLevel? RiskLevel { get; set; }

    [Range(1000, 10000000)]
    public decimal CoverageAmount { get; set; }

    public DateOnly RequestedEffectiveDate { get; set; }
}

//Data Sent to Client
public class QuoteDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public ProductType ProductType { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public decimal CoverageAmount { get; set; }
    public decimal Premium { get; set; }
    public DateOnly RequestedEffectiveDate { get; set; }
    public QuoteStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}

