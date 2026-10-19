using System.Text.RegularExpressions;
using InsuranceApi.Data;
using InsuranceApi.Dtos;
using InsuranceApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InsuranceApi.Services;

// Error = a message to show the person. NotFound = no user with that id.
public record UserResult(UserDto? User, string? Error = null, bool NotFound = false);

public interface IUserService
{
    Task EnsureSeededAsync();
    Task<AppUser?> FindForLoginAsync(string username, string password);
    Task<List<UserDto>> GetAllAsync();
    Task<UserResult> CreateAsync(CreateUserDto input);
    Task<UserResult> ResetPasswordAsync(int id, string password);
    Task<UserResult> SetActiveAsync(int id, bool isActive);
}

public class UserService : IUserService
{
    private static readonly Regex UsernameRule = new("^[a-z0-9._-]{3,30}$");

    private readonly InsuranceDbContext _context;
    private readonly PasswordHasher<AppUser> _hasher = new();

    public UserService(InsuranceDbContext context)
    {
        _context = context;
    }

    // The three demo users move into the table the first time it is empty.
    public async Task EnsureSeededAsync()
    {
        if (await _context.Users.AnyAsync()) return;

        _context.Users.AddRange(
            NewUser("superuser", "Taylor Quinn", "User123!", "SuperUser"),
            NewUser("agent", "Alex Morgan", "Agent123!", "Agent"),
            NewUser("underwriter", "Jordan Reyes", "Underwriter123!", "Underwriter"));

        await _context.SaveChangesAsync();
    }

    // Returns the user when the password is right (even if the account is switched off,
    // so the login screen can explain why). Returns null for a wrong name or password.
    public async Task<AppUser?> FindForLoginAsync(string username, string password)
    {
        var name = (username ?? "").Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == name);
        if (user == null) return null;

        var check = _hasher.VerifyHashedPassword(user, user.PasswordHash, password ?? "");
        return check == PasswordVerificationResult.Failed ? null : user;
    }

    public async Task<List<UserDto>> GetAllAsync()
    {
        var users = await _context.Users
            .AsNoTracking()
            .OrderBy(u => u.Role)
            .ThenBy(u => u.DisplayName)
            .ToListAsync();

        return users.Select(ToDto).ToList();
    }

    public async Task<UserResult> CreateAsync(CreateUserDto input)
    {
        var username = (input.Username ?? "").Trim().ToLowerInvariant();
        var displayName = (input.DisplayName ?? "").Trim();
        var role = (input.Role ?? "").Trim();

        if (displayName.Length == 0)
            return new UserResult(null, "Please enter the person's full name.");

        if (!UsernameRule.IsMatch(username))
            return new UserResult(null,
                "The username must be 3 to 30 characters: letters, numbers, dot, dash or underscore.");

        if (role != "Agent" && role != "Underwriter")
            return new UserResult(null, "The role must be Agent or Underwriter.");

        var passwordProblem = CheckPassword(input.Password);
        if (passwordProblem != null)
            return new UserResult(null, passwordProblem);

        if (await _context.Users.AnyAsync(u => u.Username == username))
            return new UserResult(null, "That username is already taken.");

        var user = NewUser(username, displayName, input.Password, role);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return new UserResult(ToDto(user));
    }

    public async Task<UserResult> ResetPasswordAsync(int id, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return new UserResult(null, NotFound: true);

        var problem = CheckPassword(password);
        if (problem != null) return new UserResult(null, problem);

        user.PasswordHash = _hasher.HashPassword(user, password);
        await _context.SaveChangesAsync();

        return new UserResult(ToDto(user));
    }

    public async Task<UserResult> SetActiveAsync(int id, bool isActive)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return new UserResult(null, NotFound: true);

        if (!isActive && user.Role == "SuperUser")
            return new UserResult(null, "The SuperUser account cannot be switched off.");

        user.IsActive = isActive;
        await _context.SaveChangesAsync();

        return new UserResult(ToDto(user));
    }

    // ----- helpers -----

    private AppUser NewUser(string username, string displayName, string password, string role)
    {
        var user = new AppUser
        {
            Username = username,
            DisplayName = displayName,
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _hasher.HashPassword(user, password);
        return user;
    }

    private static string? CheckPassword(string? password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8)
            return "The password must be at least 8 characters.";

        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            return "The password must contain at least one letter and one number.";

        return null;
    }

    private static UserDto ToDto(AppUser u) => new()
    {
        Id = u.Id,
        Username = u.Username,
        DisplayName = u.DisplayName,
        Role = u.Role,
        IsActive = u.IsActive,
        CreatedAt = u.CreatedAt
    };
}