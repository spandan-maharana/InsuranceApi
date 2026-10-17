
namespace InsuranceApi.Dtos;

public class PremiumResult
{
    public decimal Premium { get; set; }
    public bool MinimumApplied { get; set; }
    public List<PremiumStep> Steps { get; set; } = new();
}

public class PremiumStep
{
    public string Description { get; set; } = "";
    public decimal? Multiplier { get; set; }
    public decimal Amount { get; set; }
}
