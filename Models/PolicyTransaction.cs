namespace InsuranceApi.Models;

public class PolicyTransaction
{
    public int Id { get; set; }
    public int PolicyId { get; set; }
    public Policy? Policy { get; set; }
    public PolicyTransactionType Type { get; set; }
    public DateTime OccuredAt { get; set; } = DateTime.UtcNow;
    public string? PerformedBy { get; set; }
}
