using InsuranceApi.Dtos;
using InsuranceApi.Models;
using InsuranceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QuotesController : ControllerBase
{
    private readonly IQuoteService _service;

    public QuotesController(IQuoteService service)
    {
        _service = service;
    }

    // An agent sees only their own quotes.
    // The Underwriter and the SuperUser see all of them.
    [HttpGet]
    public async Task<ActionResult<List<QuoteDto>>> GetAll()
    {
        var owner = User.IsInRole("Agent") ? User.Identity?.Name : null;
        var quotes = await _service.GetAllAsync(owner);
        return Ok(quotes);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<QuoteDto>> GetById(int id)
    {
        var quote = await _service.GetByIdAsync(id);
        if (quote == null)
        {
            return NotFound();
        }
        return Ok(quote);
    }

    [HttpPost]
    [Authorize(Roles = "Agent")]
    public async Task<ActionResult<QuoteDto>> Create(QuoteInputDto input)
    {
        var created = await _service.CreateAsync(input, User.Identity?.Name);
        if (created == null)
        {
            return NotFound(new { error = "Customer not found." });
        }
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPost("{id}/quote")]
    [Authorize(Roles = "Agent")]
    public Task<ActionResult<QuoteDto>> GiveQuote(int id)
    {
        return Apply(id, QuoteAction.Quote);
    }

    [HttpPost("{id}/accept")]
    [Authorize(Roles = "Agent")]
    public Task<ActionResult<QuoteDto>> Accept(int id)
    {
        return Apply(id, QuoteAction.Accept);
    }

    [HttpPost("{id}/bind")]
    [Authorize(Roles = "Underwriter")]
    public Task<ActionResult<QuoteDto>> Bind(int id)
    {
        return Apply(id, QuoteAction.Bind);
    }

    [HttpPost("{id}/issue")]
    [Authorize(Roles = "Underwriter")]
    public Task<ActionResult<QuoteDto>> Issue(int id)
    {
        return Apply(id, QuoteAction.Issue);
    }

    [HttpPost("{id}/discard")]
    [Authorize(Roles = "Agent,Underwriter")]
    public Task<ActionResult<QuoteDto>> Discard(int id)
    {
        return Apply(id, QuoteAction.Discard);
    }

    private async Task<ActionResult<QuoteDto>> Apply(int id, QuoteAction action)
    {
        var quote = await _service.ApplyActionAsync(id, action);
        if (quote == null)
        {
            return NotFound(new { error = "Quote Not found." });
        }
        return Ok(quote);
    }
}