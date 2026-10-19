using InsuranceApi.Dtos;
using InsuranceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceApi.Controllers;

// Everything the SuperUser needs: the overview and staff users.
// The product catalogue will add its endpoints here later.
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "SuperUser")]
public class AdminController : ControllerBase
{
    private readonly IAdminOverviewService _overview;
    private readonly IUserService _users;

    public AdminController(IAdminOverviewService overview, IUserService users)
    {
        _overview = overview;
        _users = users;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<AdminOverviewDto>> GetOverview()
    {
        return Ok(await _overview.GetAsync());
    }

    [HttpGet("users")]
    public async Task<ActionResult<List<UserDto>>> GetUsers()
    {
        await _users.EnsureSeededAsync();
        return Ok(await _users.GetAllAsync());
    }

    [HttpPost("users")]
    public async Task<ActionResult<UserDto>> CreateUser(CreateUserDto input)
    {
        return ToResponse(await _users.CreateAsync(input));
    }

    [HttpPost("users/{id}/password")]
    public async Task<ActionResult<UserDto>> ResetPassword(int id, ResetPasswordDto input)
    {
        return ToResponse(await _users.ResetPasswordAsync(id, input.Password));
    }

    [HttpPost("users/{id}/active")]
    public async Task<ActionResult<UserDto>> SetActive(int id, SetActiveDto input)
    {
        return ToResponse(await _users.SetActiveAsync(id, input.IsActive));
    }

    private ActionResult<UserDto> ToResponse(UserResult result)
    {
        if (result.NotFound) return NotFound();
        if (result.Error != null) return BadRequest(new { error = result.Error });
        return Ok(result.User);
    }
}