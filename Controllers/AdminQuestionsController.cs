using InsuranceApi.Dtos;
using InsuranceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceApi.Controllers;

[ApiController]
[Route("api/admin/products/{code}/questions")]
[Authorize(Roles = "SuperUser")]
public class AdminQuestionsController : ControllerBase
{
    private readonly IAdminQuestionService _service;

    public AdminQuestionsController(IAdminQuestionService service)
    {
        _service = service;
    }

    // GET api/admin/products/{code}/questions
    [HttpGet]
    public async Task<IActionResult> GetQuestions(string code)
    {
        var questions = await _service.GetQuestionsAsync(code);
        if (questions == null)
            return NotFound(new { error = "Product not found." });

        return Ok(questions);
    }

    // POST api/admin/products/{code}/questions
    [HttpPost]
    public async Task<IActionResult> CreateQuestion(string code, [FromBody] QuestionCreateDto input)
    {
        var created = await _service.CreateQuestionAsync(code, input);
        if (created == null)
            return NotFound(new { error = "Product not found." });

        return CreatedAtAction(nameof(GetQuestions), new { code }, created);
    }

    // PUT api/admin/products/{code}/questions/{key}
    [HttpPut("{key}")]
    public async Task<IActionResult> UpdateQuestion(string code, string key, [FromBody] QuestionUpdateDto input)
    {
        var updated = await _service.UpdateQuestionAsync(code, key, input);
        if (updated == null)
            return NotFound(new { error = "Product or question not found." });

        return Ok(updated);
    }

    // DELETE api/admin/products/{code}/questions/{key}
    [HttpDelete("{key}")]
    public async Task<IActionResult> DeleteQuestion(string code, string key)
    {
        var removed = await _service.DeleteQuestionAsync(code, key);
        if (!removed)
            return NotFound(new { error = "Product or question not found." });

        return NoContent();
    }

    // POST api/admin/products/{code}/questions/{key}/rules
    [HttpPost("{key}/rules")]
    public async Task<IActionResult> AddRule(string code, string key, [FromBody] RuleInputDto input)
    {
        var rule = await _service.AddRuleAsync(code, key, input);
        if (rule == null)
            return NotFound(new { error = "Product or question not found." });

        return CreatedAtAction(nameof(GetQuestions), new { code }, rule);
    }

    // PUT api/admin/products/{code}/questions/{key}/rules/{ruleId}
    [HttpPut("{key}/rules/{ruleId:int}")]
    public async Task<IActionResult> UpdateRule(string code, string key, int ruleId, [FromBody] RuleInputDto input)
    {
        var rule = await _service.UpdateRuleAsync(code, key, ruleId, input);
        if (rule == null)
            return NotFound(new { error = "Product, question or rule not found." });

        return Ok(rule);
    }

    // DELETE api/admin/products/{code}/questions/{key}/rules/{ruleId}
    [HttpDelete("{key}/rules/{ruleId:int}")]
    public async Task<IActionResult> DeleteRule(string code, string key, int ruleId)
    {
        var removed = await _service.DeleteRuleAsync(code, key, ruleId);
        if (!removed)
            return NotFound(new { error = "Product, question or rule not found." });

        return NoContent();
    }
}