using InsuranceApi.Data;
using InsuranceApi.Models;
using InsuranceApi.Services;
using Microsoft.EntityFrameworkCore;

namespace InsuranceApi.Tests;

public class AdminOverviewServiceTests
{
    private static InsuranceDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<InsuranceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new InsuranceDbContext(options);
    }

    private static Quote MakeQuote(QuoteStatus status, string? owner, decimal? premium = null)
    {
        return new Quote
        {
            CustomerId = 1,
            ProductId = 1,
            Answers = "{}",
            CoverageAmount = 100000m,
            RequestedEffectiveDate = new DateOnly(2027, 1, 1),
            Status = status,
            Premium = premium,
            CreatedBy = owner
        };
    }

    // agent:  Draft, Quoted, Issued (1,000.50)
    // agent2: Issued (500), Discarded
    // nobody: Accepted
    private static void Seed(InsuranceDbContext db)
    {
        db.Quotes.AddRange(
            MakeQuote(QuoteStatus.Draft, "agent"),
            MakeQuote(QuoteStatus.Quoted, "agent", 900m),
            MakeQuote(QuoteStatus.Issued, "agent", 1000.50m),
            MakeQuote(QuoteStatus.Issued, "agent2", 500m),
            MakeQuote(QuoteStatus.Discarded, "agent2"),
            MakeQuote(QuoteStatus.Accepted, null, 700m));
        db.SaveChanges();
    }

    [Fact]
    public async Task EmptyDatabase_ReturnsZeros()
    {
        using var db = NewDb();

        var result = await new AdminOverviewService(db).GetAsync();

        Assert.Equal(0, result.NewRequests);
        Assert.Equal(0, result.OpenQuotes);
        Assert.Equal(0, result.IssuedPolicies);
        Assert.Equal(0m, result.PremiumIssued);
        Assert.Empty(result.Agents);
        Assert.Empty(result.RecentPolicies);
        // Every status is listed, even with a count of 0
        Assert.Equal(Enum.GetValues<QuoteStatus>().Length, result.QuotesByStatus.Count);
    }

    [Fact]
    public async Task QuotesByStatus_CountsEachStatus()
    {
        using var db = NewDb();
        Seed(db);

        var result = await new AdminOverviewService(db).GetAsync();

        int CountOf(string status) => result.QuotesByStatus.Single(s => s.Status == status).Count;

        Assert.Equal(1, CountOf("Draft"));
        Assert.Equal(1, CountOf("Quoted"));
        Assert.Equal(1, CountOf("Accepted"));
        Assert.Equal(0, CountOf("Bound"));
        Assert.Equal(2, CountOf("Issued"));
        Assert.Equal(1, CountOf("Discarded"));
    }

    [Fact]
    public async Task OpenQuotes_AreDraftQuotedAcceptedAndBound()
    {
        using var db = NewDb();
        Seed(db);

        var result = await new AdminOverviewService(db).GetAsync();

        // Draft + Quoted + Accepted
        Assert.Equal(3, result.OpenQuotes);
    }

    [Fact]
    public async Task Agents_AreGroupedByOwner_AndQuotesWithNoOwnerAreLabelled()
    {
        using var db = NewDb();
        Seed(db);

        var result = await new AdminOverviewService(db).GetAsync();

        Assert.Equal(3, result.Agents.Count);

        var agent = result.Agents.Single(a => a.Agent == "agent");
        Assert.Equal(3, agent.TotalQuotes);
        Assert.Equal(2, agent.OpenQuotes);
        Assert.Equal(1, agent.IssuedQuotes);
        Assert.Equal(1000.50m, agent.PremiumIssued);

        var agent2 = result.Agents.Single(a => a.Agent == "agent2");
        Assert.Equal(2, agent2.TotalQuotes);
        Assert.Equal(0, agent2.OpenQuotes);
        Assert.Equal(500m, agent2.PremiumIssued);

        var nobody = result.Agents.Single(a => a.Agent == AdminOverviewService.NoOwner);
        Assert.Equal(1, nobody.TotalQuotes);
        Assert.Equal(1, nobody.OpenQuotes);

        // The best-selling agent comes first
        Assert.Equal("agent", result.Agents[0].Agent);
    }
}