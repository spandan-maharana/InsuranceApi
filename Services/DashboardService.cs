using InsuranceApi.Data;
using InsuranceApi.Dtos;
using InsuranceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace InsuranceApi.Services;

public interface IDashboardService
{
    // owner = the agent's username. Pass null to include everyone's quotes (SuperUser).
    Task<DashboardDtos> GetAsync(string? owner);

    Task<int> CountNewRequestsAsync();
}

public class DashboardService : IDashboardService
{
    private readonly InsuranceDbContext _context;

    public DashboardService(InsuranceDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardDtos> GetAsync(string? owner)
    {
        var quotes = _context.Quotes.AsQueryable();
        if (owner != null)
        {
            quotes = quotes.Where(q => q.CreatedBy == owner);
        }

        var drafts = await quotes.CountAsync(q => q.Status == QuoteStatus.Draft);
        var issued = await quotes.CountAsync(q => q.Status == QuoteStatus.Issued);

        // The table: policies that came from this agent's quotes, newest first
        var policies = _context.Set<Policy>()
            .Include(p => p.Customer)
            .Include(p => p.Product)
            .AsQueryable();
        if (owner != null)
        {
            policies = policies.Where(p => p.Quote!.CreatedBy == owner);
        }

        var rows = await policies
            .OrderByDescending(p => p.CreatedAt)
            .Take(100)
            .ToListAsync();

        return new DashboardDtos
        {
            NewRequests = await CountNewRequestsAsync(),
            DraftQuotes = drafts,
            IssuedQuotes = issued,
            IssuedPolicies = rows.Select(p => new IssuedQuoteRowDto
            {
                QuoteId = p.QuoteId,
                PolicyNumber = p.PolicyNumber,
                CustomerName = $"{p.Customer?.FirstName} {p.Customer?.LastName}".Trim(),
                ProductName = p.Product?.Name ?? "",
                CoverageAmount = p.CoverageAmount,
                Premium = p.Premium,
                EffectiveDate = p.EffectiveDate,
                ExpirationDate = p.ExpirationDate,
                IssuedAt = p.CreatedAt,
                PolicyStatus = p.Status.ToString()
            }).ToList()
        };
    }

    public async Task<int> CountNewRequestsAsync()
    {
        return await _context.QuoteRequests.CountAsync(r => r.Status == RequestStatus.New);
    }
}