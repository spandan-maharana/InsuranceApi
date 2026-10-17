using InsuranceApi.Dtos;
using InsuranceApi.Models;
using InsuranceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceApi.Controllers;

// The agent's inbox. Login required.
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

    // GET api/inbox            (all, oldest first)
    // GET api/inbox?status=New (only one status: New, Accepted or Declined)
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] RequestStatus? status)
    {
        return Ok(await _service.ListAsync(status));
    }

    // GET api/inbox/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var request = await _service.GetAsync(id);
        if (request == null)
        {
            return NotFound(new { error = "Request not found." });
        }
        return Ok(request);
    }

    // POST api/inbox/5/accept
    [HttpPost("{id:int}/accept")]
    public async Task<IActionResult> Accept(int id)
    {
        var request = await _service.AcceptAsync(id);
        if (request == null)
        {
            return NotFound(new { error = "Request not found." });
        }
        return Ok(request);
    }

    // POST api/inbox/5/decline
    [HttpPost("{id:int}/decline")]
    public async Task<IActionResult> Decline(int id, [FromBody] DeclineRequestDto input)
    {
        var request = await _service.DeclineAsync(id, input);
        if (request == null)
        {
            return NotFound(new { error = "Request not found." });
        }
        return Ok(request);
    }
}