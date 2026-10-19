using InsuranceApi.Dtos;
using InsuranceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceApi.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = "Agent,SuperUser")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _service;

    public DashboardController(IDashboardService service)
    {
        _service = service;
    }

    // GET api/dashboard
    // An Agent sees their own quotes. The SuperUser sees everyone's.
    [HttpGet]
    public async Task<ActionResult<DashboardDtos>> Get()
    {
        var owner = User.IsInRole("SuperUser") ? null : User.Identity?.Name;
        return Ok(await _service.GetAsync(owner));
    }

    // GET api/dashboard/new-requests  (the number on the mail icon)
    [HttpGet("new-requests")]
    public async Task<ActionResult<NewRequestCountDto>> NewRequests()
    {
        return Ok(new NewRequestCountDto { Count = await _service.CountNewRequestsAsync() });
    }
}