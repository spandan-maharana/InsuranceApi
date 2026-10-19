using InsuranceApi.Data;
using InsuranceApi.Models;
using InsuranceApi.Services;
using Microsoft.EntityFrameworkCore;

namespace InsuranceApi.Tests;

public class CoverageCalculatorTests
{
    // A fresh database with the real catalogue and the real coverage rules
    private static InsuranceDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<InsuranceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new InsuranceDbContext(options);
        ProductCatalogLoader.SeedAsync(db).GetAwaiter().GetResult();
        return db;
    }

    private static Product LoadProduct(InsuranceDbContext db, string code)
    {
        return db.Products
            .Include(p => p.Questions)
                .ThenInclude(q => q.CoverageRules)
            .Single(p => p.Code == code);
    }

    private static Dictionary<string, string> AutoAnswers(string age, string accidents, string vehicle)
    {
        return new Dictionary<string, string>
        {
            ["driverAge"] = age,
            ["accidentsLast3Years"] = accidents,
            ["vehicleType"] = vehicle
        };
    }

    [Fact]
    public void Auto_StandardProfile_Recommends125000()
    {
        using var db = NewDb();
        var auto = LoadProduct(db, "AUTO");

        var result = CoverageCalculator.Calculate(auto, AutoAnswers("40", "0", "Sedan"));

        Assert.Equal(125000m, result.Coverage);
        Assert.False(result.LimitApplied);
    }

    [Fact]
    public void Auto_RiskierProfile_Recommends234375()
    {
        using var db = NewDb();
        var auto = LoadProduct(db, "AUTO");

        // 100,000 x 1.25 x 1.50 x 1.25 = 234,375
        var result = CoverageCalculator.Calculate(auto, AutoAnswers("30", "2", "SUV"));

        Assert.Equal(234375m, result.Coverage);
    }

    [Fact]
    public void Result_IsExactMultiplication_WithNoRoundingLine()
    {
        using var db = NewDb();
        var auto = LoadProduct(db, "AUTO");

        var result = CoverageCalculator.Calculate(auto, AutoAnswers("30", "2", "SUV"));

        // The last line of the table is the final answer, and nothing was rounded
        Assert.DoesNotContain(result.Steps, s => s.Description.StartsWith("Rounded"));
        Assert.Equal(result.Coverage, result.Steps[^1].Amount);
        Assert.Equal(4, result.Steps.Count); // start + age + accidents + vehicle
    }

    [Theory]
    [InlineData("24", 75000)]   // young driver: 100,000 x 0.75
    [InlineData("25", 125000)]  // adult starts at 25
    [InlineData("64", 125000)]  // adult ends at 64
    [InlineData("65", 100000)]  // senior starts at 65
    public void Auto_AgeBoundaries_UseTheRightRule(string age, int expected)
    {
        using var db = NewDb();
        var auto = LoadProduct(db, "AUTO");

        var result = CoverageCalculator.Calculate(auto, AutoAnswers(age, "0", "Sedan"));

        Assert.Equal((decimal)expected, result.Coverage);
    }

    [Fact]
    public void AnswerWithNoMatchingRule_IsIgnored()
    {
        using var db = NewDb();
        var auto = LoadProduct(db, "AUTO");

        // "Tractor" has no coverage rule, so only the age and accident rules apply
        var result = CoverageCalculator.Calculate(auto, AutoAnswers("40", "0", "Tractor"));

        Assert.Equal(125000m, result.Coverage);
    }

    [Fact]
    public void ResultAboveProductMaximum_IsLoweredToTheMaximum()
    {
        using var db = NewDb();
        var auto = LoadProduct(db, "AUTO");
        auto.MaxCoverage = 120000m;

        var result = CoverageCalculator.Calculate(auto, AutoAnswers("40", "0", "Sedan"));

        Assert.Equal(120000m, result.Coverage);
        Assert.True(result.LimitApplied);
    }

    [Fact]
    public void ResultBelowProductMinimum_IsRaisedToTheMinimum()
    {
        using var db = NewDb();
        var auto = LoadProduct(db, "AUTO");
        auto.MinCoverage = 200000m;

        var result = CoverageCalculator.Calculate(auto, AutoAnswers("40", "0", "Sedan"));

        Assert.Equal(200000m, result.Coverage);
        Assert.True(result.LimitApplied);
    }

    [Fact]
    public void ProductWithNoBaseCoverage_StartsFromTheProductMinimum()
    {
        using var db = NewDb();
        var auto = LoadProduct(db, "AUTO");
        auto.BaseCoverage = 0m;

        var result = CoverageCalculator.Calculate(auto, new Dictionary<string, string>());

        Assert.Equal(auto.MinCoverage, result.Coverage);
        Assert.Contains("product minimum", result.Steps[0].Description);
    }

    [Fact]
    public void EveryRealProduct_WithNoAnswers_StaysInsideItsLimits()
    {
        using var db = NewDb();
        var products = db.Products
            .Include(p => p.Questions)
                .ThenInclude(q => q.CoverageRules)
            .Where(p => p.BaseCoverage > 0)
            .ToList();

        Assert.Equal(15, products.Count);

        foreach (var product in products)
        {
            var result = CoverageCalculator.Calculate(product, new Dictionary<string, string>());

            Assert.True(result.Coverage >= product.MinCoverage,
                $"{product.Code}: {result.Coverage} is below the minimum {product.MinCoverage}");
            Assert.True(result.Coverage <= product.MaxCoverage,
                $"{product.Code}: {result.Coverage} is above the maximum {product.MaxCoverage}");
        }
    }

    [Fact]
    public async Task ApplyingDefaultsTwice_DoesNotAddRulesAgain()
    {
        using var db = NewDb();
        var before = await db.Set<CoverageRule>().CountAsync();

        await CoverageDefaults.ApplyAsync(db);

        Assert.Equal(before, await db.Set<CoverageRule>().CountAsync());
    }
}