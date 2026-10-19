using InsuranceApi.Data;
using InsuranceApi.Dtos;
using InsuranceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace InsuranceApi.Services;

public interface IAdminOverviewService
{
    Task<AdminOverviewDto> GetAsync();
}

public class AdminOverviewService : IAdminOverviewService
{
    // Quotes created before we started saving the owner have no name
    public const string NoOwner = "Not recorded (older quotes)";

    private static readonly QuoteStatus[] OpenStatuses =
    {
        QuoteStatus.Draft,
        QuoteStatus.Quoted,
        QuoteStatus.Accepted,
        QuoteStatus.Bound
    };

    private readonly InsuranceDbContext _context;

    public AdminOverviewService(InsuranceDbContext context)
    {
        _context = context;
    }

    public async Task<AdminOverviewDto> GetAsync()
    {
        // ----- Quote requests -----
        var requestCounts = await _context.Set<QuoteRequest>()
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        int RequestsWith(string status) =>
            requestCounts.Where(x => x.Status.ToString() == status).Sum(x => x.Count);

        // ----- Quotes (small projection, grouped here in memory) -----
        var quotes = await _context.Quotes
            .AsNoTracking()
            .Select(q => new { q.CreatedBy, q.Status, q.Premium })
            .ToListAsync();

        var byStatus = Enum.GetValues<QuoteStatus>()
            .Select(s => new StatusCountDto
            {
                Status = s.ToString(),
                Count = quotes.Count(q => q.Status == s)
            })
            .ToList();

        var agents = quotes
            .GroupBy(q => string.IsNullOrWhiteSpace(q.CreatedBy) ? NoOwner : q.CreatedBy!)
            .Select(g => new AgentSummaryDto
            {
                Agent = g.Key,
                TotalQuotes = g.Count(),
                OpenQuotes = g.Count(q => OpenStatuses.Contains(q.Status)),
                IssuedQuotes = g.Count(q => q.Status == QuoteStatus.Issued),
                PremiumIssued = g.Where(q => q.Status == QuoteStatus.Issued).Sum(q => q.Premium ?? 0m)
            })
            .OrderByDescending(a => a.PremiumIssued)
            .ThenByDescending(a => a.TotalQuotes)
            .ThenBy(a => a.Agent)
            .ToList();

        // ----- Policies -----
        var policies = _context.Set<Policy>();

        var issuedPolicies = await policies.CountAsync(p => p.Status == PolicyStatus.Issued);

        var premiumIssued = await policies
            .Where(p => p.Status == PolicyStatus.Issued)
            .SumAsync(p => (decimal?)p.Premium) ?? 0m;

        var recent = await policies
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .Select(p => new RecentPolicyDto
            {
                Id = p.Id,
                PolicyNumber = p.PolicyNumber,
                CustomerName = p.Customer!.FirstName + " " + p.Customer!.LastName,
                ProductName = p.Product!.Name,
                CoverageAmount = p.CoverageAmount,
                Premium = p.Premium,
                IssuedAt = p.CreatedAt
            })
            .ToListAsync();

        return new AdminOverviewDto
        {
            NewRequests = RequestsWith("New"),
            AcceptedRequests = RequestsWith("Accepted"),
            DeclinedRequests = RequestsWith("Declined"),
            OpenQuotes = quotes.Count(q => OpenStatuses.Contains(q.Status)),
            QuotesByStatus = byStatus,
            IssuedPolicies = issuedPolicies,
            PremiumIssued = premiumIssued,
            RecentPolicies = recent,
            Agents = agents
        };
    }
}