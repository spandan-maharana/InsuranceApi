namespace InsuranceApi.Dtos;

public class StatusCountDto
{
    public string Status { get; set; } = "";
    public int Count { get; set; }
}

public class AgentSummaryDto
{
    public string Agent { get; set; } = "";
    public int TotalQuotes { get; set; }
    public int OpenQuotes { get; set; }
    public int IssuedQuotes { get; set; }
    public decimal PremiumIssued { get; set; }
}

public class RecentPolicyDto
{
    public int Id { get; set; }
    public string PolicyNumber { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string ProductName { get; set; } = "";
    public decimal CoverageAmount { get; set; }
    public decimal Premium { get; set; }
    public DateTime IssuedAt { get; set; }
}

public class AdminOverviewDto
{
    // Quote requests
    public int NewRequests { get; set; }
    public int AcceptedRequests { get; set; }
    public int DeclinedRequests { get; set; }

    // Quotes: "open" means Draft, Quoted, Accepted or Bound
    public int OpenQuotes { get; set; }
    public List<StatusCountDto> QuotesByStatus { get; set; } = new();

    // Policies
    public int IssuedPolicies { get; set; }
    public decimal PremiumIssued { get; set; }
    public List<RecentPolicyDto> RecentPolicies { get; set; } = new();

    public List<AgentSummaryDto> Agents { get; set; } = new();
}

// ----- Staff users -----

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Role { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateUserDto
{
    public string Username { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Password { get; set; } = "";
    public string Role { get; set; } = "";   // Agent or Underwriter
}

public class ResetPasswordDto
{
    public string Password { get; set; } = "";
}

public class SetActiveDto
{
    public bool IsActive { get; set; }
}