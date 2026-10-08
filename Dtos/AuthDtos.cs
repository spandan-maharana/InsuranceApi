using System.ComponentModel.DataAnnotations;

namespace InsuranceApi.Dtos;

public class LoginDtos
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string Token { get; set; } = "";
    public string Role { get; set; } = "";

}
