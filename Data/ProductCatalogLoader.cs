using InsuranceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace InsuranceApi.Data;

public static class ProductCatalogLoader
{
    public static async Task SeedAsync(InsuranceDbContext context)
    {
        if (await context.ProductCategories.AnyAsync())
        {
            await CoverageDefaults.ApplyAsync(context);
            return;
        }

        var categories = new List<ProductCategory>
        {
            new ProductCategory
            {
                Code = "HEALTH", Name = "Health & Medical", SortOrder = 1,
                Products = new List<Product>
                {
                    Prod("HEALTH", "Health Insurance",
                        "Check-ups, procedures, prescriptions and hospital stays.",
                        "Annual coverage limit", 25m, 600, 100, 25000, 1000000, 1,
                        Num("age", "Age", 0, 100,
                            Between(0, 29, 0.85m, "Under 30"),
                            Between(30, 49, 1.00m, "30 to 49"),
                            Between(50, 64, 1.60m, "50 to 64"),
                            Between(65, 100, 2.20m, "65 and over")),
                        Choice("smoker", "Do you smoke?", "Yes,No",
                            Answer("Yes", 1.50m, "Smoker"),
                            Answer("No", 1.00m, "Non-smoker")),
                        Choice("plan", "Plan level", "Bronze,Silver,Gold,Platinum",
                            Answer("Bronze", 0.80m, "Bronze plan"),
                            Answer("Silver", 1.00m, "Silver plan"),
                            Answer("Gold", 1.25m, "Gold plan"),
                            Answer("Platinum", 1.50m, "Platinum plan"))),

                    Prod("DENTAL_VISION", "Dental & Vision",
                        "Cleanings, dental work, eye exams and eyewear.",
                        "Annual benefit limit", 120m, 150, 20, 500, 5000, 2,
                        Num("age", "Age", 0, 100,
                            Between(0, 17, 0.80m, "Child"),
                            Between(18, 64, 1.00m, "Adult"),
                            Between(65, 100, 1.30m, "Senior")),
                        Choice("coverageType", "What do you want covered?", "Dental only,Vision only,Dental and vision",
                            Answer("Dental only", 0.70m, "Dental only"),
                            Answer("Vision only", 0.40m, "Vision only"),
                            Answer("Dental and vision", 1.00m, "Dental and vision"))),

                    Prod("DISABILITY", "Disability Insurance",
                        "Replaces part of your income if illness or injury stops you working.",
                        "Monthly benefit", 200m, 300, 50, 1000, 15000, 3,
                        Num("age", "Age", 18, 64,
                            Between(18, 34, 0.90m, "Under 35"),
                            Between(35, 49, 1.00m, "35 to 49"),
                            Between(50, 64, 1.50m, "50 to 64")),
                        Choice("occupation", "Type of work", "Office work,Skilled trade,Manual labor",
                            Answer("Office work", 1.00m, "Office work"),
                            Answer("Skilled trade", 1.40m, "Skilled trade"),
                            Answer("Manual labor", 1.90m, "Manual labor")),
                        Choice("waitingPeriodDays", "Waiting period before payments start (days)", "30,90,180",
                            Answer("30", 1.25m, "30-day wait"),
                            Answer("90", 1.00m, "90-day wait"),
                            Answer("180", 0.80m, "180-day wait"))),

                    Prod("LTC", "Long-Term Care Insurance",
                        "Nursing home, assisted living or in-home care costs.",
                        "Total benefit pool", 12m, 800, 100, 50000, 1000000, 4,
                        Num("age", "Age", 40, 79,
                            Between(40, 54, 0.70m, "40 to 54"),
                            Between(55, 64, 1.00m, "55 to 64"),
                            Between(65, 79, 2.20m, "65 to 79")),
                        Choice("healthStatus", "General health", "Excellent,Good,Fair",
                            Answer("Excellent", 0.90m, "Excellent health"),
                            Answer("Good", 1.00m, "Good health"),
                            Answer("Fair", 1.40m, "Fair health"))),

                    Prod("SUPPLEMENTAL", "Supplemental Health",
                        "Cash payments for accidents, critical illness or hospital stays.",
                        "Lump-sum benefit", 18m, 120, 20, 2000, 50000, 5,
                        Num("age", "Age", 18, 80,
                            Between(18, 39, 0.80m, "Under 40"),
                            Between(40, 59, 1.00m, "40 to 59"),
                            Between(60, 80, 1.70m, "60 and over")),
                        Choice("smoker", "Do you smoke?", "Yes,No",
                            Answer("Yes", 1.40m, "Smoker"),
                            Answer("No", 1.00m, "Non-smoker")),
                        Choice("planType", "Type of cover", "Accident,Critical illness,Hospital indemnity",
                            Answer("Accident", 0.70m, "Accident cover"),
                            Answer("Critical illness", 1.30m, "Critical illness cover"),
                            Answer("Hospital indemnity", 1.00m, "Hospital indemnity cover")))
                }
            },

            new ProductCategory
            {
                Code = "LIFE", Name = "Life Insurance", SortOrder = 2,
                Products = new List<Product>
                {
                    Prod("TERM_LIFE", "Term Life Insurance",
                        "Pays your family if you pass away within a set number of years.",
                        "Death benefit", 1.20m, 150, 25, 25000, 5000000, 1,
                        Num("age", "Age", 18, 80,
                            Between(18, 30, 0.80m, "18 to 30"),
                            Between(31, 45, 1.00m, "31 to 45"),
                            Between(46, 60, 1.90m, "46 to 60"),
                            Between(61, 80, 3.50m, "61 and over")),
                        Choice("smoker", "Do you smoke?", "Yes,No",
                            Answer("Yes", 2.00m, "Smoker"),
                            Answer("No", 1.00m, "Non-smoker")),
                        Choice("termYears", "Length of cover (years)", "10,20,30",
                            Answer("10", 0.85m, "10-year term"),
                            Answer("20", 1.00m, "20-year term"),
                            Answer("30", 1.25m, "30-year term"))),

                    Prod("PERMANENT_LIFE", "Permanent Life Insurance",
                        "Lifelong cover with a savings (cash value) part.",
                        "Death benefit", 9m, 600, 50, 25000, 5000000, 2,
                        Num("age", "Age", 18, 70,
                            Between(18, 30, 0.80m, "18 to 30"),
                            Between(31, 45, 1.00m, "31 to 45"),
                            Between(46, 60, 1.80m, "46 to 60"),
                            Between(61, 70, 3.00m, "61 and over")),
                        Choice("smoker", "Do you smoke?", "Yes,No",
                            Answer("Yes", 2.00m, "Smoker"),
                            Answer("No", 1.00m, "Non-smoker")),
                        Choice("policyType", "Type of permanent cover", "Whole life,Universal life",
                            Answer("Whole life", 1.00m, "Whole life"),
                            Answer("Universal life", 0.85m, "Universal life")))
                }
            },

            new ProductCategory
            {
                Code = "PROPERTY", Name = "Property & Casualty", SortOrder = 3,
                Products = new List<Product>
                {
                    Prod("AUTO", "Auto Insurance",
                        "Vehicle damage, repairs and legal liability from accidents.",
                        "Liability coverage", 15m, 300, 50, 10000, 500000, 1,
                        Num("driverAge", "Driver age", 16, 100,
                            Between(16, 24, 1.60m, "Young driver"),
                            Between(25, 64, 1.00m, "Adult driver"),
                            Between(65, 100, 1.20m, "Senior driver")),
                        Num("accidentsLast3Years", "Accidents in the last 3 years", 0, 10,
                            Between(0, 0, 0.90m, "Clean record"),
                            Between(1, 1, 1.25m, "One accident"),
                            Between(2, 10, 1.75m, "Two or more accidents")),
                        Choice("vehicleType", "Vehicle type", "Sedan,SUV,Truck,Sports car",
                            Answer("Sedan", 1.00m, "Sedan"),
                            Answer("SUV", 1.10m, "SUV"),
                            Answer("Truck", 1.05m, "Truck"),
                            Answer("Sports car", 1.40m, "Sports car"))),

                    Prod("HOME", "Homeowners Insurance",
                        "Your house, belongings and liability if someone is hurt on your property.",
                        "Dwelling coverage", 5m, 400, 75, 50000, 2000000, 2,
                        Num("yearBuilt", "Year the home was built", 1800, 2030,
                            Between(1800, 1969, 1.30m, "Built before 1970"),
                            Between(1970, 1999, 1.10m, "Built 1970 to 1999"),
                            Between(2000, 2030, 0.95m, "Built 2000 or later")),
                        Choice("roofType", "Roof type", "Asphalt,Metal,Tile,Wood",
                            Answer("Asphalt", 1.00m, "Asphalt roof"),
                            Answer("Metal", 0.90m, "Metal roof"),
                            Answer("Tile", 0.95m, "Tile roof"),
                            Answer("Wood", 1.25m, "Wood roof")),
                        Choice("hasAlarm", "Does the home have a security alarm?", "Yes,No",
                            Answer("Yes", 0.95m, "Security alarm"),
                            Answer("No", 1.00m, "No alarm"))),

                    Prod("RENTERS", "Renters Insurance",
                        "Your belongings and temporary living costs if a rental is damaged.",
                        "Personal property coverage", 6m, 120, 15, 5000, 200000, 3,
                        Choice("buildingType", "Type of home", "Apartment,House,Condo",
                            Answer("Apartment", 1.00m, "Apartment"),
                            Answer("House", 1.15m, "House"),
                            Answer("Condo", 0.95m, "Condo")),
                        Choice("hasAlarm", "Does the home have a security alarm?", "Yes,No",
                            Answer("Yes", 0.90m, "Security alarm"),
                            Answer("No", 1.00m, "No alarm")),
                        Num("claimsLast5Years", "Claims in the last 5 years", 0, 10,
                            Between(0, 0, 0.95m, "No claims"),
                            Between(1, 1, 1.20m, "One claim"),
                            Between(2, 10, 1.60m, "Two or more claims"))),

                    Prod("SPECIALTY_VEHICLE", "Specialized Vehicle Insurance",
                        "Motorcycles, boats, RVs, ATVs and classic cars.",
                        "Vehicle value", 25m, 200, 40, 2000, 300000, 4,
                        Choice("vehicleType", "Vehicle type", "Motorcycle,Boat,RV,ATV,Classic car",
                            Answer("Motorcycle", 1.30m, "Motorcycle"),
                            Answer("Boat", 1.10m, "Boat"),
                            Answer("RV", 0.90m, "RV"),
                            Answer("ATV", 1.40m, "ATV"),
                            Answer("Classic car", 0.80m, "Classic car")),
                        Num("ownerAge", "Owner age", 16, 100,
                            Between(16, 24, 1.70m, "Young owner"),
                            Between(25, 64, 1.00m, "Adult owner"),
                            Between(65, 100, 1.15m, "Senior owner")),
                        Choice("storage", "Where is it kept?", "Garage,Outdoors",
                            Answer("Garage", 0.90m, "Kept in a garage"),
                            Answer("Outdoors", 1.10m, "Kept outdoors"))),

                    Prod("UMBRELLA", "Umbrella Insurance",
                        "Extra liability cover on top of your auto and home policies.",
                        "Extra liability limit", 0.35m, 150, 25, 500000, 10000000, 5,
                        Num("numberOfVehicles", "Number of vehicles in the household", 0, 10,
                            Between(0, 2, 1.00m, "Up to 2 vehicles"),
                            Between(3, 10, 1.30m, "3 or more vehicles")),
                        Choice("hasPool", "Is there a swimming pool?", "Yes,No",
                            Answer("Yes", 1.20m, "Swimming pool"),
                            Answer("No", 1.00m, "No pool")),
                        Num("youngDrivers", "Drivers under 25 in the household", 0, 10,
                            Between(0, 0, 1.00m, "No young drivers"),
                            Between(1, 10, 1.40m, "Has young drivers")))
                }
            },

            new ProductCategory
            {
                Code = "SPECIALIZED", Name = "Specialized & Niche", SortOrder = 4,
                Products = new List<Product>
                {
                    Prod("PET", "Pet Insurance",
                        "Vet bills for unexpected illness or injury.",
                        "Annual vet-bill limit", 70m, 150, 15, 1000, 20000, 1,
                        Choice("species", "Type of pet", "Dog,Cat",
                            Answer("Dog", 1.20m, "Dog"),
                            Answer("Cat", 1.00m, "Cat")),
                        Num("petAge", "Pet age (years)", 0, 20,
                            Between(0, 2, 0.90m, "Young pet"),
                            Between(3, 7, 1.00m, "Adult pet"),
                            Between(8, 20, 1.80m, "Senior pet")),
                        Choice("breedRisk", "Breed health risk", "Low,Medium,High",
                            Answer("Low", 0.85m, "Low-risk breed"),
                            Answer("Medium", 1.00m, "Medium-risk breed"),
                            Answer("High", 1.40m, "High-risk breed"))),

                    Prod("TRAVEL", "Travel Insurance",
                        "Pays back trip costs if you must cancel, and covers emergencies abroad.",
                        "Trip cost", 45m, 30, 10, 500, 50000, 2,
                        Num("travelerAge", "Traveler age", 0, 100,
                            Between(0, 39, 0.90m, "Under 40"),
                            Between(40, 64, 1.00m, "40 to 64"),
                            Between(65, 100, 1.90m, "65 and over")),
                        Num("tripLengthDays", "Trip length (days)", 1, 365,
                            Between(1, 7, 0.80m, "Short trip"),
                            Between(8, 21, 1.00m, "Medium trip"),
                            Between(22, 365, 1.60m, "Long trip")),
                        Choice("destination", "Destination", "Domestic,International",
                            Answer("Domestic", 1.00m, "Domestic trip"),
                            Answer("International", 1.30m, "International trip"))),

                    Prod("COMMERCIAL", "Commercial / Business Insurance",
                        "Business liability, property damage and professional errors.",
                        "Liability limit", 10m, 750, 150, 100000, 5000000, 3,
                        Choice("industry", "Type of business", "Office,Retail,Restaurant,Construction",
                            Answer("Office", 1.00m, "Office business"),
                            Answer("Retail", 1.20m, "Retail business"),
                            Answer("Restaurant", 1.50m, "Restaurant"),
                            Answer("Construction", 2.50m, "Construction business")),
                        Num("employees", "Number of employees", 1, 500,
                            Between(1, 10, 1.00m, "Up to 10 employees"),
                            Between(11, 50, 1.40m, "11 to 50 employees"),
                            Between(51, 500, 2.00m, "51 or more employees")),
                        Num("yearsInBusiness", "Years in business", 0, 100,
                            Between(0, 1, 1.50m, "New business"),
                            Between(2, 5, 1.15m, "2 to 5 years"),
                            Between(6, 100, 1.00m, "6 or more years")))
                }
            }
        };

        context.ProductCategories.AddRange(categories);
        await context.SaveChangesAsync();

        await CoverageDefaults.ApplyAsync(context);
    }


    private static Product Prod(string code, string name, string description, string coverageLabel,
        decimal ratePerThousand, decimal minimumPremium, decimal policyFee,
        decimal minCoverage, decimal maxCoverage, int sortOrder,
        params ProductQuestion[] questions)
    {
        for (var i = 0; i < questions.Length; i++)
        {
            questions[i].SortOrder = i + 1;
        }

        return new Product
        {
            Code = code,
            Name = name,
            Description = description,
            CoverageLabel = coverageLabel,
            RatePerThousand = ratePerThousand,
            MinimumPremium = minimumPremium,
            PolicyFee = policyFee,
            MinCoverage = minCoverage,
            MaxCoverage = maxCoverage,
            SortOrder = sortOrder,
            Questions = questions.ToList()
        };
    }

    private static ProductQuestion Num(string key, string label, decimal min, decimal max, params RatingRules[] rules)
    {
        return new ProductQuestion
        {
            Key = key,
            Label = label,
            Type = QuestionType.Number,
            MinValue = min,
            MaxValue = max,
            Rules = rules.ToList()
        };
    }

    private static ProductQuestion Choice(string key, string label, string options, params RatingRules[] rules)
    {
        return new ProductQuestion
        {
            Key = key,
            Label = label,
            Type = QuestionType.Choice,
            Options = options,
            Rules = rules.ToList()
        };
    }

    private static RatingRules Between(decimal from, decimal to, decimal multiplier, string description)
    {
        return new RatingRules { FromValue = from, ToValue = to, Multiplier = multiplier, Description = description };
    }

    private static RatingRules Answer(string answer, decimal multiplier, string description)
    {
        return new RatingRules { MatchValue = answer, Multiplier = multiplier, Description = description };
    }

}
