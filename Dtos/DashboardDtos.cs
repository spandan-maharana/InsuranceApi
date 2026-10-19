namespace InsuranceApi.Dtos;

public class IssuedQuoteRowDto
{
    public int QuoteId { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal CoverageAmount { get; set; }
    public decimal Premium { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public DateOnly ExpirationDate { get; set; }
    public DateTime IssuedAt { get; set; }
    public string PolicyStatus { get; set; } = string.Empty;
}
public class DashboardDtos
{
    public int NewRequests { get; set; }
    public int DraftQuotes { get; set; }
    public int IssuedQuotes { get; set; }
    public List<IssuedQuoteRowDto> IssuedPolicies { get; set; } = new();
}
public class NewRequestCountDto
{
    public int Count { get; set; }
}