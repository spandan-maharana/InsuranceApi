namespace InsuranceApi.Dtos;

public class CoverageLine
{
    public string Description { get; set; } = string.Empty;
    public decimal? Multiplier { get; set; }
    public decimal Amount { get; set; }
}

public class CoverageResult
{
    public decimal Coverage { get; set; }
    public bool LimitApplied { get; set; }
    public List<CoverageLine> Steps { get; set; } = new();
}
