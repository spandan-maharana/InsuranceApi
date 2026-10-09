using InsuranceApi.Dtos;
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

    [HttpGet]
    public async Task<ActionResult<List<QuoteDto>>> GetAll()
    {
        var quotes = await _service.GetAllAsync();
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
    public async Task<ActionResult<QuoteDto>> Create(QuoteInputDto input)
    {
        var created = await _service.CreateAsync(input);
        if (created == null)
        {
            return NotFound(new { error = "Customer not found." });
        }
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}