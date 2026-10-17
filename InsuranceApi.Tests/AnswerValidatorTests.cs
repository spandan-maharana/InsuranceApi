using System.Text.Json;
using InsuranceApi.Data;
using InsuranceApi.Dtos;
using InsuranceApi.Models;
using InsuranceApi.Services;
using Xunit;

namespace InsuranceApi.Tests;

public class AnswerValidatorTests
{
    // A small Auto product with three questions
    private static Product BuildProduct()
    {
        return new Product
        {
            Code = "AUTO",
            Name = "Auto Insurance",
            Questions = new List<ProductQuestion>
            {
                new ProductQuestion
                {
                    Key = "driverAge", Label = "Driver age", Type = QuestionType.Number,
                    IsRequired = true, MinValue = 16, MaxValue = 100, SortOrder = 1
                },
                new ProductQuestion
                {
                    Key = "vehicleType", Label = "Vehicle type", Type = QuestionType.Choice,
                    IsRequired = true, Options = "Sedan,SUV,Truck,Sports car", SortOrder = 2
                },
                new ProductQuestion
                {
                    Key = "nickname", Label = "Car nickname", Type = QuestionType.Choice,
                    IsRequired = false, Options = "Yes,No", SortOrder = 3
                }
            }
        };
    }

    // Turns raw JSON text into a JsonElement, the way the API receives it
    private static JsonElement Json(string raw)
    {
        return JsonDocument.Parse(raw).RootElement.Clone();
    }

    [Fact]
    public void Good_answers_come_back_as_clean_text()
    {
        var input = new Dictionary<string, JsonElement>
        {
            ["driverAge"] = Json("40"),
            ["vehicleType"] = Json("\"sports car\"")
        };

        var result = AnswerValidator.Validate(BuildProduct(), input);

        Assert.Equal("40", result["driverAge"]);
        Assert.Equal("Sports car", result["vehicleType"]);   // proper spelling from the list
    }

    [Fact]
    public void A_number_sent_as_text_is_accepted()
    {
        var input = new Dictionary<string, JsonElement>
        {
            ["driverAge"] = Json("\"40\""),
            ["vehicleType"] = Json("\"Sedan\"")
        };

        var result = AnswerValidator.Validate(BuildProduct(), input);

        Assert.Equal("40", result["driverAge"]);
    }

    [Fact]
    public void A_missing_required_answer_is_refused()
    {
        var input = new Dictionary<string, JsonElement>
        {
            ["vehicleType"] = Json("\"Sedan\"")
        };

        var error = Assert.Throws<BadRequestException>(() => AnswerValidator.Validate(BuildProduct(), input));

        Assert.Contains("Driver age", error.Message);
    }

    [Fact]
    public void An_optional_question_can_be_left_out()
    {
        var input = new Dictionary<string, JsonElement>
        {
            ["driverAge"] = Json("40"),
            ["vehicleType"] = Json("\"Sedan\"")
        };

        var result = AnswerValidator.Validate(BuildProduct(), input);

        Assert.False(result.ContainsKey("nickname"));
    }

    [Fact]
    public void An_answer_for_an_unknown_question_is_refused()
    {
        var input = new Dictionary<string, JsonElement>
        {
            ["driverAge"] = Json("40"),
            ["vehicleType"] = Json("\"Sedan\""),
            ["shoeSize"] = Json("9")
        };

        var error = Assert.Throws<BadRequestException>(() => AnswerValidator.Validate(BuildProduct(), input));

        Assert.Contains("shoeSize", error.Message);
    }

    [Fact]
    public void A_number_outside_the_range_is_refused()
    {
        var input = new Dictionary<string, JsonElement>
        {
            ["driverAge"] = Json("12"),
            ["vehicleType"] = Json("\"Sedan\"")
        };

        var error = Assert.Throws<BadRequestException>(() => AnswerValidator.Validate(BuildProduct(), input));

        Assert.Contains("between 16 and 100", error.Message);
    }

    [Fact]
    public void Text_where_a_number_is_expected_is_refused()
    {
        var input = new Dictionary<string, JsonElement>
        {
            ["driverAge"] = Json("\"abc\""),
            ["vehicleType"] = Json("\"Sedan\"")
        };

        var error = Assert.Throws<BadRequestException>(() => AnswerValidator.Validate(BuildProduct(), input));

        Assert.Contains("must be a number", error.Message);
    }

    [Fact]
    public void A_choice_that_is_not_on_the_list_is_refused()
    {
        var input = new Dictionary<string, JsonElement>
        {
            ["driverAge"] = Json("40"),
            ["vehicleType"] = Json("\"Spaceship\"")
        };

        var error = Assert.Throws<BadRequestException>(() => AnswerValidator.Validate(BuildProduct(), input));

        Assert.Contains("must be one of", error.Message);
    }
}