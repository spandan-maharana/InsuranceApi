using InsuranceApi.Models;
using InsuranceApi.Services;
using Xunit;

namespace InsuranceApi.Tests;

public class RatingCalculatorTests
{
    // ---- Test products built in memory (no database) ----

    // Auto: $15 per $1,000, $50 fee, $300 minimum
    private static Product BuildAuto()
    {
        return new Product
        {
            Code = "AUTO",
            Name = "Auto Insurance",
            RatePerThousand = 15m,
            PolicyFee = 50m,
            MinimumPremium = 300m,
            Questions = new List<ProductQuestion>
            {
                new ProductQuestion
                {
                    Key = "driverAge", Type = QuestionType.Number, SortOrder = 1,
                    Rules = new List<RatingRules>
                    {
                        new RatingRules { FromValue = 16, ToValue = 24, Multiplier = 1.60m, Description = "Young driver" },
                        new RatingRules { FromValue = 25, ToValue = 64, Multiplier = 1.00m, Description = "Adult driver" },
                        new RatingRules { FromValue = 65, ToValue = 100, Multiplier = 1.20m, Description = "Senior driver" }
                    }
                },
                new ProductQuestion
                {
                    Key = "accidentsLast3Years", Type = QuestionType.Number, SortOrder = 2,
                    Rules = new List<RatingRules>
                    {
                        new RatingRules { FromValue = 0, ToValue = 0, Multiplier = 0.90m, Description = "Clean record" },
                        new RatingRules { FromValue = 1, ToValue = 1, Multiplier = 1.25m, Description = "One accident" },
                        new RatingRules { FromValue = 2, ToValue = 10, Multiplier = 1.75m, Description = "Two or more accidents" }
                    }
                },
                new ProductQuestion
                {
                    Key = "vehicleType", Type = QuestionType.Choice, SortOrder = 3,
                    Rules = new List<RatingRules>
                    {
                        new RatingRules { MatchValue = "Sedan", Multiplier = 1.00m, Description = "Sedan" },
                        new RatingRules { MatchValue = "SUV", Multiplier = 1.10m, Description = "SUV" },
                        new RatingRules { MatchValue = "Truck", Multiplier = 1.05m, Description = "Truck" },
                        new RatingRules { MatchValue = "Sports car", Multiplier = 1.40m, Description = "Sports car" }
                    }
                }
            }
        };
    }

    // A simple product with one age question: $10 per $1,000, no fee, no minimum
    private static Product BuildAgeOnlyProduct()
    {
        return new Product
        {
            Code = "TEST",
            RatePerThousand = 10m,
            PolicyFee = 0m,
            MinimumPremium = 0m,
            Questions = new List<ProductQuestion>
            {
                new ProductQuestion
                {
                    Key = "driverAge", Type = QuestionType.Number, SortOrder = 1,
                    Rules = new List<RatingRules>
                    {
                        new RatingRules { FromValue = 16, ToValue = 24, Multiplier = 1.60m, Description = "Young" },
                        new RatingRules { FromValue = 25, ToValue = 64, Multiplier = 1.00m, Description = "Adult" },
                        new RatingRules { FromValue = 65, ToValue = 100, Multiplier = 1.20m, Description = "Senior" }
                    }
                }
            }
        };
    }

    private static Dictionary<string, string> Answers(string age, string accidents, string vehicle)
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["driverAge"] = age,
            ["accidentsLast3Years"] = accidents,
            ["vehicleType"] = vehicle
        };
    }

    // ---- Tests ----

    [Fact]
    public void Clean_sedan_driver_pays_1130()
    {
        // 80 x 15 = 1200, x1.00 x0.90 x1.00 = 1080, + 50 fee
        var result = RatingCalculator.Calculate(BuildAuto(), 80000m, Answers("40", "0", "Sedan"));

        Assert.Equal(1130.00m, result.Premium);
        Assert.False(result.MinimumApplied);
    }

    [Fact]
    public void Young_risky_driver_in_a_sports_car_pays_4754()
    {
        // 1200 x1.60 x1.75 x1.40 = 4704, + 50 fee (the fee is not multiplied)
        var result = RatingCalculator.Calculate(BuildAuto(), 80000m, Answers("20", "2", "Sports car"));

        Assert.Equal(4754.00m, result.Premium);
    }

    [Theory]
    [InlineData("16", 1600.00)]
    [InlineData("24", 1600.00)]
    [InlineData("25", 1000.00)]
    [InlineData("64", 1000.00)]
    [InlineData("65", 1200.00)]
    [InlineData("100", 1200.00)]
    public void Number_rules_include_both_ends_of_the_range(string age, double expected)
    {
        var answers = new Dictionary<string, string> { ["driverAge"] = age };

        var result = RatingCalculator.Calculate(BuildAgeOnlyProduct(), 100000m, answers);

        Assert.Equal((decimal)expected, result.Premium);
    }

    [Fact]
    public void Choice_answer_is_not_case_sensitive()
    {
        // 1200 x1.00 x0.90 x1.40 = 1512, + 50 fee
        var result = RatingCalculator.Calculate(BuildAuto(), 80000m, Answers("40", "0", "sports car"));

        Assert.Equal(1562.00m, result.Premium);
    }

    [Fact]
    public void A_question_with_no_answer_is_skipped()
    {
        // Only age given: 1200 x1.00 = 1200, + 50 fee
        var answers = new Dictionary<string, string> { ["driverAge"] = "40" };

        var result = RatingCalculator.Calculate(BuildAuto(), 80000m, answers);

        Assert.Equal(1250.00m, result.Premium);
    }

    [Fact]
    public void An_answer_that_matches_no_rule_changes_nothing()
    {
        // Age 150 is outside every range, so only the base price applies
        var answers = new Dictionary<string, string> { ["driverAge"] = "150" };

        var result = RatingCalculator.Calculate(BuildAgeOnlyProduct(), 100000m, answers);

        Assert.Equal(1000.00m, result.Premium);
    }

    [Fact]
    public void Minimum_premium_lifts_a_tiny_policy()
    {
        var product = new Product
        {
            Code = "SMALL",
            RatePerThousand = 10m,
            PolicyFee = 10m,
            MinimumPremium = 100m
        };

        // Base 10 + fee 10 = 20, which is below the 100 minimum
        var result = RatingCalculator.Calculate(product, 1000m, new Dictionary<string, string>());

        Assert.Equal(100.00m, result.Premium);
        Assert.True(result.MinimumApplied);
    }

    [Fact]
    public void Breakdown_lists_every_step_and_ends_at_the_final_premium()
    {
        var result = RatingCalculator.Calculate(BuildAuto(), 80000m, Answers("40", "0", "Sedan"));

        // Base, age, accidents, vehicle, fee
        Assert.Equal(5, result.Steps.Count);
        Assert.StartsWith("Base", result.Steps.First().Description);
        Assert.Equal(result.Premium, result.Steps.Last().Amount);
    }
}