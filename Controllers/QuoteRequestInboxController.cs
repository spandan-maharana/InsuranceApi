using InsuranceApi.Dtos;
using InsuranceApi.Models;
using InsuranceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace InsuranceApi.Controllers;

[ApiController]
[Route("api/inbox")]
[Authorize(Roles = "Agent,SuperUser")]
public class QuoteRequestInboxController : ControllerBase
{
    private readonly IQuoteRequestService _service;

    public QuoteRequestInboxController(IQuoteRequestService service)
    {
        _service = service;
    }

    // GET api/inbox            -> all requests
    // GET api/inbox?status=New -> only requests with that status
    [HttpGet]
    public async Task<ActionResult<List<QuoteRequestDto>>> List([FromQuery] RequestStatus? status)
    {
        return Ok(await _service.ListAsync(status));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<QuoteRequestDto>> Get(int id)
    {
        var request = await _service.GetAsync(id);
        if (request == null)
        {
            return NotFound(new { error = "Request not found." });
        }

        return Ok(request);
    }

    [Authorize(Roles = "Agent")]
    [HttpPost("{id:int}/accept")]
    public async Task<ActionResult<QuoteRequestDto>> Accept(
        int id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] AcceptRequestDto? input)
    {
        var request = await _service.AcceptAsync(id, input, User.Identity?.Name);
        if (request == null)
        {
            return NotFound(new { error = "Request not found." });
        }

        return Ok(request);
    }

    [Authorize(Roles = "Agent")]
    [HttpPost("{id:int}/decline")]
    public async Task<ActionResult<QuoteRequestDto>> Decline(int id, [FromBody] DeclineRequestDto input)
    {
        var request = await _service.DeclineAsync(id, input);
        if (request == null)
        {
            return NotFound(new { error = "Request not found." });
        }

        return Ok(request);
    }
}