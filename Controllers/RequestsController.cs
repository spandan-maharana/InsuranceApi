using InsuranceApi.Dtos;
using InsuranceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace InsuranceApi.Controllers;

[ApiController]
[Route("api/requests")]
[AllowAnonymous]
[EnableRateLimiting("PublicRequests")]
public class RequestsController : ControllerBase
{
    private readonly IQuoteRequestService _service;

    public RequestsController(IQuoteRequestService service)
    {
        _service = service;
    }

    // POST api/requests
    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] RequestInputDto input)
    {
        var receipt = await _service.SubmitAsync(input);
        return StatusCode(StatusCodes.Status201Created, receipt);
    }
}