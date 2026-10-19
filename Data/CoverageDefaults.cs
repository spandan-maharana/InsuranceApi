using InsuranceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace InsuranceApi.Data;

public static class CoverageDefaults
{
    public static async Task ApplyAsync(InsuranceDbContext context)
    {
        var products = await context.Products
            .Include(p => p.Questions)
                .ThenInclude(q => q.CoverageRules)
            .Where(p => p.BaseCoverage == 0)
            .ToListAsync();

        var setups = BuildSetups();
        var changed = false;

        foreach (var product in products)
        {
            if (!setups.TryGetValue(product.Code, out var setup))
            {
                continue;
            }

            product.BaseCoverage = setup.BaseCoverage;
            product.CoverageStep = setup.Step;

            foreach (var (questionKey, rule) in setup.Rules)
            {
                var question = product.Questions.FirstOrDefault(q => q.Key == questionKey);
                question?.CoverageRules.Add(rule);
            }

            changed = true;
        }

        if (changed)
        {
            await context.SaveChangesAsync();
        }
    }

    private sealed class Setup
    {
        public decimal BaseCoverage { get; init; }
        public decimal Step { get; init; }
        public List<(string QuestionKey, CoverageRule Rule)> Rules { get; init; } = new();
    }

    // Built fresh every time, so each database gets its own rule objects
    private static Dictionary<string, Setup> BuildSetups()
    {
        return new Dictionary<string, Setup>
        {
            ["HEALTH"] = S(100000, 5000,
                Between("age", 0, 29, 0.75m, "Under 30: lower expected costs"),
                Between("age", 30, 49, 1.00m, "30 to 49: standard limit"),
                Between("age", 50, 64, 1.75m, "50 to 64: higher expected costs"),
                Between("age", 65, 100, 2.50m, "65 and over: highest expected costs"),
                Answer("smoker", "Yes", 1.25m, "Smoker: higher limit suggested"),
                Answer("smoker", "No", 1.00m, "Non-smoker: standard limit"),
                Answer("plan", "Bronze", 0.75m, "Bronze plan"),
                Answer("plan", "Silver", 1.00m, "Silver plan"),
                Answer("plan", "Gold", 1.50m, "Gold plan"),
                Answer("plan", "Platinum", 2.00m, "Platinum plan")),

            ["DENTAL_VISION"] = S(1500, 250,
                Between("age", 0, 17, 0.80m, "Child"),
                Between("age", 18, 64, 1.00m, "Adult"),
                Between("age", 65, 100, 1.20m, "Senior"),
                Answer("coverageType", "Dental only", 1.00m, "Dental only"),
                Answer("coverageType", "Vision only", 0.50m, "Vision only"),
                Answer("coverageType", "Dental and vision", 1.50m, "Dental and vision")),

            ["DISABILITY"] = S(3000, 250,
                Between("age", 18, 34, 0.90m, "Under 35: lower benefit"),
                Between("age", 35, 49, 1.10m, "35 to 49: standard benefit"),
                Between("age", 50, 64, 1.20m, "50 to 64: higher benefit"),
                Answer("occupation", "Office work", 1.25m, "Office work: higher benefit available"),
                Answer("occupation", "Skilled trade", 1.00m, "Skilled trade: standard benefit"),
                Answer("occupation", "Manual labor", 0.80m, "Manual labor: lower benefit limit")),

            ["LTC"] = S(150000, 10000,
                Between("age", 40, 54, 0.80m, "40 to 54: lower pool"),
                Between("age", 55, 64, 1.20m, "55 to 64: larger pool"),
                Between("age", 65, 79, 1.60m, "65 to 79: largest pool"),
                Answer("healthStatus", "Excellent", 1.00m, "Excellent health"),
                Answer("healthStatus", "Good", 1.20m, "Good health"),
                Answer("healthStatus", "Fair", 1.50m, "Fair health: more care expected")),

            ["SUPPLEMENTAL"] = S(10000, 1000,
                Between("age", 18, 39, 0.80m, "Under 40"),
                Between("age", 40, 59, 1.00m, "40 to 59"),
                Between("age", 60, 80, 1.30m, "60 and over"),
                Answer("planType", "Accident", 0.50m, "Accident cover"),
                Answer("planType", "Critical illness", 2.00m, "Critical illness cover"),
                Answer("planType", "Hospital indemnity", 1.00m, "Hospital indemnity cover")),

            ["TERM_LIFE"] = S(250000, 25000,
                Between("age", 18, 30, 1.00m, "18 to 30: starting out"),
                Between("age", 31, 45, 2.00m, "31 to 45: peak family need"),
                Between("age", 46, 60, 1.50m, "46 to 60"),
                Between("age", 61, 80, 0.50m, "61 and over: fewer dependents"),
                Answer("termYears", "10", 0.80m, "10-year term"),
                Answer("termYears", "20", 1.00m, "20-year term"),
                Answer("termYears", "30", 1.20m, "30-year term")),

            ["PERMANENT_LIFE"] = S(150000, 25000,
                Between("age", 18, 30, 1.00m, "18 to 30: starting out"),
                Between("age", 31, 45, 1.80m, "31 to 45: peak family need"),
                Between("age", 46, 60, 1.50m, "46 to 60"),
                Between("age", 61, 70, 0.80m, "61 and over: fewer dependents"),
                Answer("policyType", "Whole life", 1.00m, "Whole life"),
                Answer("policyType", "Universal life", 1.20m, "Universal life")),

            ["AUTO"] = S(100000, 5000,
                Between("driverAge", 16, 24, 0.75m, "Young driver: starter limit"),
                Between("driverAge", 25, 64, 1.25m, "Adult driver: more assets to protect"),
                Between("driverAge", 65, 100, 1.00m, "Senior driver: standard limit"),
                Between("accidentsLast3Years", 0, 0, 1.00m, "No accidents: standard limit"),
                Between("accidentsLast3Years", 1, 1, 1.25m, "One accident: higher limit suggested"),
                Between("accidentsLast3Years", 2, 10, 1.50m, "Two or more accidents: higher limit suggested"),
                Answer("vehicleType", "Sedan", 1.00m, "Sedan"),
                Answer("vehicleType", "SUV", 1.25m, "SUV: heavier vehicle"),
                Answer("vehicleType", "Truck", 1.50m, "Truck: heaviest vehicle"),
                Answer("vehicleType", "Sports car", 1.75m, "Sports car: higher speed and value")),

            ["HOME"] = S(300000, 10000,
                Between("yearBuilt", 1800, 1969, 0.90m, "Built before 1970: smaller home on average"),
                Between("yearBuilt", 1970, 1999, 1.00m, "Built 1970 to 1999"),
                Between("yearBuilt", 2000, 2030, 1.30m, "Built 2000 or later: larger home on average"),
                Answer("roofType", "Asphalt", 1.00m, "Asphalt roof"),
                Answer("roofType", "Metal", 1.10m, "Metal roof: costs more to replace"),
                Answer("roofType", "Tile", 1.15m, "Tile roof: costs more to replace"),
                Answer("roofType", "Wood", 1.10m, "Wood roof: costs more to replace")),

            ["RENTERS"] = S(30000, 5000,
                Answer("buildingType", "Apartment", 1.00m, "Apartment"),
                Answer("buildingType", "House", 1.50m, "House: more belongings"),
                Answer("buildingType", "Condo", 1.25m, "Condo")),

            ["SPECIALTY_VEHICLE"] = S(20000, 1000,
                Answer("vehicleType", "Motorcycle", 0.75m, "Motorcycle: typical value"),
                Answer("vehicleType", "Boat", 3.00m, "Boat: typical value"),
                Answer("vehicleType", "RV", 5.00m, "RV: typical value"),
                Answer("vehicleType", "ATV", 0.50m, "ATV: typical value"),
                Answer("vehicleType", "Classic car", 2.50m, "Classic car: typical value")),

            ["UMBRELLA"] = S(1000000, 250000,
                Between("numberOfVehicles", 0, 2, 1.00m, "Up to 2 vehicles"),
                Between("numberOfVehicles", 3, 10, 1.50m, "3 or more vehicles: more exposure"),
                Answer("hasPool", "Yes", 1.50m, "Swimming pool: more exposure"),
                Answer("hasPool", "No", 1.00m, "No pool"),
                Between("youngDrivers", 0, 0, 1.00m, "No young drivers"),
                Between("youngDrivers", 1, 10, 1.50m, "Has young drivers: more exposure")),

            ["PET"] = S(5000, 500,
                Answer("species", "Dog", 1.20m, "Dog"),
                Answer("species", "Cat", 1.00m, "Cat"),
                Between("petAge", 0, 2, 1.00m, "Young pet"),
                Between("petAge", 3, 7, 1.00m, "Adult pet"),
                Between("petAge", 8, 20, 1.50m, "Senior pet: higher limit suggested"),
                Answer("breedRisk", "Low", 0.80m, "Low-risk breed"),
                Answer("breedRisk", "Medium", 1.00m, "Medium-risk breed"),
                Answer("breedRisk", "High", 1.50m, "High-risk breed: higher limit suggested")),

            ["TRAVEL"] = S(3000, 500,
                Answer("destination", "Domestic", 1.00m, "Domestic trip"),
                Answer("destination", "International", 2.00m, "International trip: costs more"),
                Between("tripLengthDays", 1, 7, 1.00m, "Short trip"),
                Between("tripLengthDays", 8, 21, 2.00m, "Medium trip"),
                Between("tripLengthDays", 22, 365, 4.00m, "Long trip")),

            ["COMMERCIAL"] = S(500000, 50000,
                Answer("industry", "Office", 1.00m, "Office business"),
                Answer("industry", "Retail", 1.50m, "Retail business"),
                Answer("industry", "Restaurant", 2.00m, "Restaurant"),
                Answer("industry", "Construction", 3.00m, "Construction business: highest risk"),
                Between("employees", 1, 10, 1.00m, "Up to 10 employees"),
                Between("employees", 11, 50, 2.00m, "11 to 50 employees"),
                Between("employees", 51, 500, 4.00m, "51 or more employees"))
        };
    }

    private static Setup S(decimal baseCoverage, decimal step, params (string Key, CoverageRule Rule)[] rules)
    {
        return new Setup { BaseCoverage = baseCoverage, Step = step, Rules = rules.ToList() };
    }

    private static (string Key, CoverageRule Rule) Between(
        string key, decimal from, decimal to, decimal multiplier, string description)
    {
        return (key, new CoverageRule
        {
            FromValue = from,
            ToValue = to,
            Multiplier = multiplier,
            Description = description
        });
    }

    private static (string Key, CoverageRule Rule) Answer(
        string key, string answer, decimal multiplier, string description)
    {
        return (key, new CoverageRule
        {
            MatchValue = answer,
            Multiplier = multiplier,
            Description = description
        });
    }
}