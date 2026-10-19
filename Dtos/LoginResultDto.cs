using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace InsuranceApi.Dtos;

public class LoginResultDto : LoginResponseDto
{
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}
