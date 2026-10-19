namespace InsuranceApi.Models;

public class AppUser
{
    public int Id { get; set; }
    public string Username { get; set; } = "";      // always saved in lowercase
    public string DisplayName { get; set; } = "";   // shown on screen, e.g. "Alex Morgan"
    public string PasswordHash { get; set; } = "";  // never the real password
    public string Role { get; set; } = "";          // Agent, Underwriter or SuperUser
    public bool IsActive { get; set; } = true;      // false = switched off, cannot log in
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}