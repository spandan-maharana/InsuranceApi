using InsuranceApi.Data;
using InsuranceApi.Dtos;
using InsuranceApi.Services;
using Microsoft.EntityFrameworkCore;

namespace InsuranceApi.Tests;

public class UserServiceTests
{
    private static InsuranceDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<InsuranceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new InsuranceDbContext(options);
    }

    private static CreateUserDto NewAgent(string username = "sam.lee") => new()
    {
        Username = username,
        DisplayName = "Sam Lee",
        Password = "Welcome123",
        Role = "Agent"
    };

    [Fact]
    public async Task EnsureSeeded_AddsThreeUsers_OnlyOnce()
    {
        using var db = NewDb();
        var service = new UserService(db);

        await service.EnsureSeededAsync();
        await service.EnsureSeededAsync();

        Assert.Equal(3, await db.Users.CountAsync());
    }

    [Fact]
    public async Task FindForLogin_CorrectPassword_ReturnsTheUser()
    {
        using var db = NewDb();
        var service = new UserService(db);
        await service.EnsureSeededAsync();

        var user = await service.FindForLoginAsync("agent", "Agent123!");

        Assert.NotNull(user);
        Assert.Equal("Agent", user!.Role);
        Assert.Equal("Alex Morgan", user.DisplayName);
    }

    [Fact]
    public async Task FindForLogin_WrongPassword_ReturnsNull()
    {
        using var db = NewDb();
        var service = new UserService(db);
        await service.EnsureSeededAsync();

        Assert.Null(await service.FindForLoginAsync("agent", "nope"));
    }

    [Fact]
    public async Task FindForLogin_UnknownUser_ReturnsNull()
    {
        using var db = NewDb();
        var service = new UserService(db);
        await service.EnsureSeededAsync();

        Assert.Null(await service.FindForLoginAsync("ghost", "Agent123!"));
    }

    [Fact]
    public async Task Create_ValidAgent_IsSavedInLowercase_AndCanLogIn()
    {
        using var db = NewDb();
        var service = new UserService(db);

        var result = await service.CreateAsync(NewAgent("  Sam.Lee "));

        Assert.Null(result.Error);
        Assert.Equal("sam.lee", result.User!.Username);

        var user = await service.FindForLoginAsync("SAM.LEE", "Welcome123");
        Assert.NotNull(user);
        Assert.NotEqual("Welcome123", user!.PasswordHash); // stored hashed
    }

    [Fact]
    public async Task Create_DuplicateUsername_IsRejected()
    {
        using var db = NewDb();
        var service = new UserService(db);
        await service.CreateAsync(NewAgent());

        var again = await service.CreateAsync(NewAgent());

        Assert.NotNull(again.Error);
        Assert.Equal(1, await db.Users.CountAsync());
    }

    [Fact]
    public async Task Create_SuperUserRole_IsRejected()
    {
        using var db = NewDb();
        var service = new UserService(db);
        var input = NewAgent();
        input.Role = "SuperUser";

        var result = await service.CreateAsync(input);

        Assert.NotNull(result.Error);
        Assert.Equal(0, await db.Users.CountAsync());
    }

    [Fact]
    public async Task Create_WeakPassword_IsRejected()
    {
        using var db = NewDb();
        var service = new UserService(db);
        var input = NewAgent();
        input.Password = "short1";

        var result = await service.CreateAsync(input);

        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task ResetPassword_OldStopsWorking_NewWorks()
    {
        using var db = NewDb();
        var service = new UserService(db);
        var created = await service.CreateAsync(NewAgent());

        var result = await service.ResetPasswordAsync(created.User!.Id, "Another456");

        Assert.Null(result.Error);
        Assert.Null(await service.FindForLoginAsync("sam.lee", "Welcome123"));
        Assert.NotNull(await service.FindForLoginAsync("sam.lee", "Another456"));
    }

    [Fact]
    public async Task SetActive_SwitchesAgentOff_ButNeverTheSuperUser()
    {
        using var db = NewDb();
        var service = new UserService(db);
        await service.EnsureSeededAsync();

        var agent = await db.Users.SingleAsync(u => u.Username == "agent");
        var boss = await db.Users.SingleAsync(u => u.Username == "superuser");

        var off = await service.SetActiveAsync(agent.Id, false);
        Assert.Null(off.Error);
        Assert.False(off.User!.IsActive);

        var blocked = await service.SetActiveAsync(boss.Id, false);
        Assert.NotNull(blocked.Error);
        Assert.True((await db.Users.FindAsync(boss.Id))!.IsActive);
    }
}