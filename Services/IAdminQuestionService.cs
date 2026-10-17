using InsuranceApi.Dtos;

namespace InsuranceApi.Services;

public interface IAdminQuestionService
{
    // All questions (with their rules) for one product. Null if the product does not exist.
    Task<List<AdminQuestionDtos>?> GetQuestionsAsync(string productCode);

    // Add a question. Null if the product does not exist.
    Task<AdminQuestionDtos?> CreateQuestionAsync(string productCode, QuestionCreateDto input);

    // Edit a question. Null if the product or question does not exist.
    Task<AdminQuestionDtos?> UpdateQuestionAsync(string productCode, string key, QuestionUpdateDto input);

    // Remove a question. False if the product or question does not exist.
    Task<bool> DeleteQuestionAsync(string productCode, string key);

    // Add a rule to a question. Null if the product or question does not exist.
    Task<AdminRuleDto?> AddRuleAsync(string productCode, string key, RuleInputDto input);

    // Edit a rule. Null if the product, question or rule does not exist.
    Task<AdminRuleDto?> UpdateRuleAsync(string productCode, string key, int ruleId, RuleInputDto input);

    // Remove a rule. False if the product, question or rule does not exist.
    Task<bool> DeleteRuleAsync(string productCode, string key, int ruleId);
}