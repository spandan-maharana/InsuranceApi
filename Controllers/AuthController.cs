using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InsuranceApi.Dtos;
using InsuranceApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace InsuranceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IUserService _users;

    public AuthController(IConfiguration config, IUserService users)
    {
        _config = config;
        _users = users;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResultDto>> Login(LoginDtos login)
    {
        // The three demo users are copied into the Users table the first time.
        await _users.EnsureSeededAsync();

        var user = await _users.FindForLoginAsync(login.Username, login.Password);
        if (user == null)
        {
            return Unauthorized(new { Error = "Invalid username or password" });
        }

        if (!user.IsActive)
        {
            return Unauthorized(new { Error = "This account is switched off. Please contact your administrator." });
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new LoginResultDto
        {
            Token = jwt,
            Role = user.Role,
            Username = user.Username,
            DisplayName = user.DisplayName
        });
    }
}