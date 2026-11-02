namespace InsuranceApi.Models;

public class QuoteHistory
{
    public int Id { get; set; }
    public int QuoteId { get; set; }
    public Quote? Quote { get; set; }
    public string Action { get; set; } = "";
    public QuoteStatus FromStatus { get; set; }
    public QuoteStatus ToStatus { get; set; }
    public DateTime OccuredAt { get; set; } = DateTime.UtcNow;
    public string? PerformedBy { get; set; }
}
