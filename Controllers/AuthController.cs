using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InsuranceApi.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace InsuranceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;

    public AuthController(IConfiguration config)
    {
        _config = config;
    }

    [HttpPost("login")]
    public ActionResult<LoginResponseDto> Login(LoginDtos login)
    {
        // For demonstration, I am using two fixed users.
        // In a real application, we will validate the user against a database
        // Containing a Users table with hashed passwords.
        string? role = null;
        if(login.Username == "superuser" && login.Password == "User123!")
        {
            role = "SuperUser";

        }
        else if (login.Username == "agent" && login.Password == "Agent123!")
        {
            role = "Agent";
        }
        if (role == null)
        {
            return Unauthorized(new {Error = "Invalid username or password"});
        }
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, login.Username),
            new Claim(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new LoginResponseDto { Token = jwt, Role = role });
    }
}
