using System.Text.Json;
using InsuranceApi.Data;
using InsuranceApi.Dtos;
using InsuranceApi.Models;
using InsuranceApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace InsuranceApi.Tests;

public class DashboardServiceTests
{
    private static InsuranceDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<InsuranceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new InsuranceDbContext(options);
        ProductCatalogLoader.SeedAsync(db).GetAwaiter().GetResult();
        return db;
    }

    private static QuoteRequestService NewRequestService(InsuranceDbContext db)
    {
        return new QuoteRequestService(db, NullLogger<QuoteRequestService>.Instance);
    }

    private static RequestInputDto GoodInput(string email)
    {
        return new RequestInputDto
        {
            FirstName = "Maya",
            LastName = "Rivera",
            Email = email,
            PhoneNumber = "603-555-0142",
            ProductCode = "AUTO",
            RequestedEffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(1),
            Answers = new Dictionary<string, JsonElement>
            {
                ["driverAge"] = JsonSerializer.SerializeToElement(40),
                ["accidentsLast3Years"] = JsonSerializer.SerializeToElement(0),
                ["vehicleType"] = JsonSerializer.SerializeToElement("Sedan")
            }
        };
    }

    // Submits a request and accepts it as the given agent. Returns the new quote's id.
    private static async Task<int> AcceptAs(InsuranceDbContext db, string email, string? agent)
    {
        var service = NewRequestService(db);
        var receipt = await service.SubmitAsync(GoodInput(email));
        var accepted = await service.AcceptAsync(receipt.Id, null, agent);
        return accepted!.QuoteId!.Value;
    }

    // Makes a quote Issued and gives it a policy, like the real flow would
    private static async Task IssueQuote(InsuranceDbContext db, int quoteId, string policyNumber)
    {
        var quote = await db.Quotes.SingleAsync(q => q.Id == quoteId);
        quote.Status = QuoteStatus.Issued;
        db.Set<Policy>().Add(new Policy
        {
            PolicyNumber = policyNumber,
            Segment = 1,
            Term = 1,
            CustomerId = quote.CustomerId,
            QuoteId = quote.Id,
            ProductId = quote.ProductId,
            CoverageAmount = quote.CoverageAmount,
            Premium = 900m,
            EffectiveDate = quote.RequestedEffectiveDate,
            ExpirationDate = quote.RequestedEffectiveDate.AddYears(1)
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Accept_WithAgentName_SetsTheQuoteOwner()
    {
        using var db = NewDb();

        await AcceptAs(db, "a@example.com", "agent");

        Assert.Equal("agent", (await db.Quotes.SingleAsync()).CreatedBy);
    }

    [Fact]
    public async Task Accept_WithoutAgentName_LeavesTheOwnerEmpty()
    {
        using var db = NewDb();

        await AcceptAs(db, "a@example.com", null);

        Assert.Null((await db.Quotes.SingleAsync()).CreatedBy);
    }

    [Fact]
    public async Task Dashboard_CountsOnlyThisAgentsDraftQuotes()
    {
        using var db = NewDb();
        await AcceptAs(db, "a@example.com", "agent");
        await AcceptAs(db, "b@example.com", "agent");
        await AcceptAs(db, "c@example.com", "someone.else");

        var dashboard = await new DashboardService(db).GetAsync("agent");

        Assert.Equal(2, dashboard.DraftQuotes);
        Assert.Equal(0, dashboard.IssuedQuotes);
    }

    [Fact]
    public async Task Dashboard_WithNoOwnerFilter_CountsEveryonesQuotes()
    {
        using var db = NewDb();
        await AcceptAs(db, "a@example.com", "agent");
        await AcceptAs(db, "c@example.com", "someone.else");

        var dashboard = await new DashboardService(db).GetAsync(null);

        Assert.Equal(2, dashboard.DraftQuotes);
    }

    [Fact]
    public async Task Dashboard_NewRequests_CountsOnlyRequestsStillNew()
    {
        using var db = NewDb();
        var service = NewRequestService(db);
        var a = await service.SubmitAsync(GoodInput("a@example.com"));
        await service.SubmitAsync(GoodInput("b@example.com"));
        await service.SubmitAsync(GoodInput("c@example.com"));
        await service.AcceptAsync(a.Id, null, "agent");

        var dashboard = await new DashboardService(db).GetAsync("agent");

        Assert.Equal(2, dashboard.NewRequests);
        Assert.Equal(2, await new DashboardService(db).CountNewRequestsAsync());
    }

    [Fact]
    public async Task Dashboard_IssuedTable_ShowsOnlyThisAgentsPolicies()
    {
        using var db = NewDb();
        var mine = await AcceptAs(db, "a@example.com", "agent");
        var theirs = await AcceptAs(db, "c@example.com", "someone.else");
        await IssueQuote(db, mine, "POL-MINE");
        await IssueQuote(db, theirs, "POL-THEIRS");

        var dashboard = await new DashboardService(db).GetAsync("agent");

        Assert.Equal(1, dashboard.IssuedQuotes);
        var row = Assert.Single(dashboard.IssuedPolicies);
        Assert.Equal("POL-MINE", row.PolicyNumber);
        Assert.Equal("Maya Rivera", row.CustomerName);
        Assert.Equal("Auto Insurance", row.ProductName);
        Assert.Equal(125000m, row.CoverageAmount);
        Assert.Equal(900m, row.Premium);
    }

    [Fact]
    public async Task Dashboard_IssuedQuote_IsNoLongerCountedAsDraft()
    {
        using var db = NewDb();
        var quoteId = await AcceptAs(db, "a@example.com", "agent");
        await AcceptAs(db, "b@example.com", "agent");
        await IssueQuote(db, quoteId, "POL-1");

        var dashboard = await new DashboardService(db).GetAsync("agent");

        Assert.Equal(1, dashboard.DraftQuotes);
        Assert.Equal(1, dashboard.IssuedQuotes);
    }
}