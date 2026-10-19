using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using InsuranceApi.Models;

namespace InsuranceApi.Dtos;

// What a member of the public sends through the request form
public class RequestInputDto
{
    // ----- About the applicant -----
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(30, MinimumLength = 7)]
    [RegularExpression(@"^[0-9+()\-\s]+$",
        ErrorMessage = "Phone number can only have digits, spaces, + ( ) and -.")]
    public string PhoneNumber { get; set; } = string.Empty;

    // ----- What they want covered -----
    [Required]
    [StringLength(30)]
    public string ProductCode { get; set; } = string.Empty;

    public DateOnly RequestedEffectiveDate { get; set; }

    // The product's questions, e.g. { "riderAge": 20, "storage": "Garage" }
    public Dictionary<string, JsonElement>? Answers { get; set; }
}

// What the public gets back. Nothing else is shown to them.
public class RequestReceivedDto
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
}

// What an agent sees in the inbox
public class QuoteRequestDto
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal CoverageAmount { get; set; }
    public string CoverageLabel { get; set; } = string.Empty;
    public decimal MinCoverage { get; set; }
    public decimal MaxCoverage { get; set; }
    public CoverageResult? CoverageBreakdown { get; set; }

    public DateOnly RequestedEffectiveDate { get; set; }
    public Dictionary<string, string> Answers { get; set; } = new();

    public RequestStatus Status { get; set; }
    public string? DeclineReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }

    // Filled in once the request has been accepted
    public int? CustomerId { get; set; }
    public int? QuoteId { get; set; }
}

public class AcceptRequestDto
{
    [Range(1, 1000000000)]
    public decimal? CoverageAmount { get; set; }
}

// What an agent sends to decline a request
public class DeclineRequestDto
{
    [Required]
    [StringLength(500, MinimumLength = 3)]
    public string Reason { get; set; } = string.Empty;
}