using InsuranceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceApi.Controllers;

[ApiController]
[Route("api/policies")]
[Authorize]
public class PoliciesController : ControllerBase
{
    private readonly IPolicyService _policyService;

    public PoliciesController(IPolicyService policyService)
    {
        _policyService = policyService;
    }

    // Any logged-in user can read. Optional search: GET /api/policies?segment=1
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? segment)
    {
        var policies = await _policyService.GetAllAsync(segment);
        return Ok(policies);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var policy = await _policyService.GetByIdAsync(id);
        if (policy == null)
        {
            return NotFound(new { error = "Policy not found." });
        }
        return Ok(policy);
    }

    // NEW: the policy that was created from a quote
    [HttpGet("by-quote/{quoteId}")]
    public async Task<IActionResult> GetByQuote(int quoteId)
    {
        var policy = await _policyService.GetByQuoteIdAsync(quoteId);
        if (policy == null)
        {
            return NotFound(new { error = "No policy exists for this quote." });
        }
        return Ok(policy);
    }

    // Only the Agent renews and cancels
    [HttpPost("{id}/renew")]
    [Authorize(Roles = "Agent")]
    public async Task<IActionResult> Renew(int id)
    {
        var policy = await _policyService.RenewAsync(id);
        if (policy == null)
        {
            return NotFound(new { error = "Policy not found." });
        }
        return Ok(policy);
    }

    [HttpPost("{id}/cancel")]
    [Authorize(Roles = "Agent")]
    public async Task<IActionResult> Cancel(int id)
    {
        var policy = await _policyService.CancelAsync(id);
        if (policy == null)
        {
            return NotFound(new { error = "Policy not found." });
        }
        return Ok(policy);
    }
}