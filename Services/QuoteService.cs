using InsuranceApi.Data;
using InsuranceApi.Dtos;
using InsuranceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace InsuranceApi.Services;

public class QuoteService : IQuoteService
{
    private readonly InsuranceDbContext _context;
    private readonly ILogger<QuoteService> _logger;

    public QuoteService(InsuranceDbContext context, ILogger<QuoteService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<QuoteDto>> GetAllAsync()
    {
        var quotes = await _context.Quotes
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();
        return quotes.Select(ToDto).ToList();
    }

    public async Task<QuoteDto?> GetByIdAsync(int id)
    {
        var quote = await _context.Quotes.FirstOrDefaultAsync(q => q.Id == id);
        if (quote == null)
        {
            return null;
        }
        return ToDto(quote);
    }

    // Returns null when the customer does not exist
    public async Task<QuoteDto?> CreateAsync(QuoteInputDto input)
    {
        var customerExists = await _context.Customers.AnyAsync(c => c.Id == input.customerId);
        if (!customerExists)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (input.RequestedEffectiveDate < today)
        {
            throw new BadRequestException("The effective date cannot be in the past.");
        }

        var quote = new Quote
        {
            CustomerId = input.customerId,
            ProductType = input.ProductType!.Value,
            RiskLevel = input.RiskLevel!.Value,
            CoverageAmount = input.CoverageAmount,
            RequestedEffectiveDate = input.RequestedEffectiveDate,
            Premium = PremiumCalculator.Calculate(
                input.ProductType!.Value, input.RiskLevel!.Value, input.CoverageAmount),
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        _context.Quotes.Add(quote);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Created quote {QuoteId} for customer {CustomerId}", quote.Id, quote.CustomerId);

        return ToDto(quote);
    }

    private static QuoteDto ToDto(Quote q)
    {
        return new QuoteDto
        {
            Id = q.Id,
            CustomerId = q.CustomerId,
            ProductType = q.ProductType,
            RiskLevel = q.RiskLevel,
            CoverageAmount = q.CoverageAmount,
            Premium = q.Premium,
            RequestedEffectiveDate = q.RequestedEffectiveDate,
            Status = q.Status,
            CreatedAt = q.CreatedAt,
            ExpiresAt = q.ExpiresAt
        };
    }
}
