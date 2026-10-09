using InsuranceApi.Models;
namespace InsuranceApi.Services;

public static class PremiumCalculator
{
    public static decimal Calculate(ProductType product, RiskLevel risk, decimal coverageAmount)
    {
        decimal baseRate = product switch
        {
            ProductType.Auto => 0.015m,
            ProductType.Home => 0.005m,
            ProductType.Life => 0.004m,
            ProductType.Health => 0.02m,
            _ => throw new ArgumentOutOfRangeException(nameof(product))
        };

        decimal riskMultiplier = risk switch
        {
            RiskLevel.Low => 0.8m,
            RiskLevel.Medium => 1.0m,
            RiskLevel.High => 1.5m,
            _ => throw new ArgumentOutOfRangeException(nameof(risk))
        };

        return Math.Round(coverageAmount * baseRate * riskMultiplier, 2);
    }
}

//Premium = coverage * rate for the product * multiplier for risk

//Ex: A Home Policy of $300,000 at Medium Risk, the premium = 300000 * 0.005 * 1 = $1500

