using InsuranceApi.Data;
using InsuranceApi.Dtos;
using InsuranceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace InsuranceApi.Services;

public class PolicyService : IPolicyService
{
    private const int FirstSegment = 1;

    private readonly InsuranceDbContext _context;
    private readonly ILogger<PolicyService> _logger;

    public PolicyService(InsuranceDbContext context, ILogger<PolicyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<PolicyDto>> GetAllAsync(int? segment)
    {
        var query = _context.Policies
            .Include(p => p.Product)
            .Include(p => p.Customer)
            .AsQueryable();

        if (segment != null)
        {
            query = query.Where(p => p.Segment == segment);
        }

        var policies = await query
            .OrderBy(p => p.Segment)
            .ThenBy(p => p.Term)
            .ToListAsync();

        return policies.Select(ToDto).ToList();
    }

    public async Task<PolicyDto?> GetByIdAsync(int id)
    {
        var policy = await _context.Policies
            .Include(p => p.Product)
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(p => p.Id == id);
        return policy == null ? null : ToDto(policy);
    }

    public async Task<PolicyDto?> GetByQuoteIdAsync(int quoteId)
    {
        var policy = await _context.Policies
            .Include(p => p.Product)
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(p => p.QuoteId == quoteId && p.Term == 0);
        return policy == null ? null : ToDto(policy);
    }

    public async Task<Policy> CreateFromQuoteAsync(Quote quote, string? performedBy = null)
    {
        if (quote.Premium == null)
        {
            throw new BadRequestException("A policy cannot be created from a quote with no premium.");
        }

        var highest = await _context.Policies.MaxAsync(p => (int?)p.Segment);
        var segment = (highest ?? (FirstSegment - 1)) + 1;

        var policy = new Policy
        {
            Segment = segment,
            Term = 0,
            PolicyNumber = BuildNumber(segment, 0),
            CustomerId = quote.CustomerId,
            QuoteId = quote.Id,
            ProductId = quote.ProductId,
            CoverageAmount = quote.CoverageAmount,
            Premium = quote.Premium.Value,
            EffectiveDate = quote.RequestedEffectiveDate,
            ExpirationDate = quote.RequestedEffectiveDate.AddYears(1),
            Status = PolicyStatus.Issued
        };

        _context.Policies.Add(policy);
        _context.PolicyTransactions.Add(new PolicyTransaction
        {
            Policy = policy,
            Type = PolicyTransactionType.Issued,
            PerformedBy = performedBy
        });

        _logger.LogInformation("Policy {PolicyNumber} created from quote {QuoteId}", policy.PolicyNumber, quote.Id);
        return policy;
    }

    public async Task<PolicyDto?> RenewAsync(int id, string? performedBy = null)
    {
        var old = await _context.Policies
            .Include(p => p.Product)
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (old == null) { return null; }

        if (old.Status != PolicyStatus.Issued)
        {
            throw new ConflictException("Only an Issued policy can be renewed.");
        }

        var nextTerm = old.Term + 1;

        var alreadyRenewed = await _context.Policies
            .AnyAsync(p => p.Segment == old.Segment && p.Term == nextTerm);
        if (alreadyRenewed)
        {
            throw new ConflictException($"Policy {old.PolicyNumber} has already been renewed.");
        }

        var renewal = new Policy
        {
            Segment = old.Segment,
            Term = nextTerm,
            PolicyNumber = BuildNumber(old.Segment, nextTerm),
            CustomerId = old.CustomerId,
            Customer = old.Customer,
            QuoteId = old.QuoteId,
            ProductId = old.ProductId,
            Product = old.Product,
            CoverageAmount = old.CoverageAmount,
            Premium = old.Premium,   // same price for now
            EffectiveDate = old.ExpirationDate,
            ExpirationDate = old.ExpirationDate.AddYears(1),
            Status = PolicyStatus.Issued
        };

        _context.Policies.Add(renewal);
        _context.PolicyTransactions.Add(new PolicyTransaction
        {
            Policy = renewal,
            Type = PolicyTransactionType.Renewed,
            PerformedBy = performedBy
        });
        await _context.SaveChangesAsync();

        _logger.LogInformation("Policy {Old} renewed as {New}", old.PolicyNumber, renewal.PolicyNumber);
        return ToDto(renewal);
    }

    public async Task<PolicyDto?> CancelAsync(int id, string? performedBy = null)
    {
        var policy = await _context.Policies
            .Include(p => p.Product)
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (policy == null) { return null; }

        if (policy.Status == PolicyStatus.Cancelled)
        {
            throw new ConflictException("This policy is already cancelled.");
        }

        policy.Status = PolicyStatus.Cancelled;
        policy.CancelledAt = DateTime.UtcNow;
        policy.UpdatedAt = DateTime.UtcNow;
        _context.PolicyTransactions.Add(new PolicyTransaction
        {
            PolicyId = policy.Id,
            Type = PolicyTransactionType.Cancelled,
            PerformedBy = performedBy
        });
        await _context.SaveChangesAsync();

        _logger.LogInformation("Policy {PolicyNumber} cancelled", policy.PolicyNumber);
        return ToDto(policy);
    }

    private static string BuildNumber(int segment, int term)
    {
        return $"P-{segment:D6}-{term:D2}";
    }

    private static PolicyDto ToDto(Policy p)
    {
        return new PolicyDto
        {
            Id = p.Id,
            PolicyNumber = p.PolicyNumber,
            Segment = p.Segment,
            Term = p.Term,
            CustomerId = p.CustomerId,
            CustomerName = p.Customer != null ? $"{p.Customer.FirstName} {p.Customer.LastName}" : "",
            QuoteId = p.QuoteId,
            ProductCode = p.Product!.Code,
            ProductName = p.Product.Name,
            CoverageAmount = p.CoverageAmount,
            Premium = p.Premium,
            EffectiveDate = p.EffectiveDate,
            ExpirationDate = p.ExpirationDate,
            Status = p.Status,
            CancelledAt = p.CancelledAt,
            CreatedAt = p.CreatedAt
        };
    }
}