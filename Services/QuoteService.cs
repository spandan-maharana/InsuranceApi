using System.Text.Json;
using InsuranceApi.Data;
using InsuranceApi.Dtos;
using InsuranceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace InsuranceApi.Services;

public class QuoteService : IQuoteService
{
    private readonly InsuranceDbContext _context;
    private readonly ILogger<QuoteService> _logger;
    private readonly IPolicyService _policyService;

    public QuoteService(InsuranceDbContext context, ILogger<QuoteService> logger, IPolicyService policyService)
    {
        _context = context;
        _logger = logger;
        _policyService = policyService;
    }

    public async Task<List<QuoteDto>> GetAllAsync()
    {
        var quotes = await _context.Quotes
            .Include(q => q.Product)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();

        var changed = false;
        foreach (var q in quotes)
        {
            if (ExpireIfNeeded(q))
            {
                changed = true;
            }
        }
        if (changed)
        {
            await _context.SaveChangesAsync();
        }
        return quotes.Select(ToDto).ToList();
    }

    public async Task<QuoteDto?> GetByIdAsync(int id)
    {
        var quote = await _context.Quotes
            .Include(q => q.Product)
            .FirstOrDefaultAsync(q => q.Id == id);
        if (quote == null)
        {
            return null;
        }
        if (ExpireIfNeeded(quote))
        {
            await _context.SaveChangesAsync();
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

        // Find the product, together with its questions
        var code = input.ProductCode!.Trim().ToUpperInvariant();
        var product = await _context.Products
            .Include(p => p.Questions)
            .FirstOrDefaultAsync(p => p.Code == code && p.IsActive);
        if (product == null)
        {
            throw new BadRequestException("Unknown or unavailable product.");
        }

        // The coverage must fit this product's limits
        if (input.CoverageAmount < product.MinCoverage || input.CoverageAmount > product.MaxCoverage)
        {
            throw new BadRequestException(
                $"{product.CoverageLabel} for {product.Name} must be between {product.MinCoverage:N0} and {product.MaxCoverage:N0}.");
        }

        // Check the answers against the product's questions
        var answers = AnswerValidator.Validate(product, input.Answers);

        var quote = new Quote
        {
            CustomerId = input.customerId,
            ProductId = product.Id,
            Product = product,
            Answers = JsonSerializer.Serialize(answers),
            CoverageAmount = input.CoverageAmount,
            RequestedEffectiveDate = input.RequestedEffectiveDate
        };

        _context.Quotes.Add(quote);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Created quote {QuoteId} for customer {CustomerId}", quote.Id, quote.CustomerId);

        return ToDto(quote);
    }

    public async Task<QuoteDto?> ApplyActionAsync(int id, QuoteAction action)
    {
        var quote = await _context.Quotes
            .Include(q => q.Product)
            .FirstOrDefaultAsync(q => q.Id == id);
        if (quote == null)
        {
            return null;
        }
        if (ExpireIfNeeded(quote))
        {
            await _context.SaveChangesAsync();
        }

        var newStatus = QuoteLifeCycle.Apply(action, quote.Status);

        if (action == QuoteAction.Quote)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (quote.RequestedEffectiveDate < today)
            {
                throw new BadRequestException("The effective date cannot be in the past.");
            }

            if (quote.CoverageAmount <= 0)
            {
                throw new BadRequestException("Coverage amount is required before a quote can be given.");
            }

            // Load the product with its questions and rules, so the calculator can price it
            var product = await _context.Products
                .Include(p => p.Questions)
                    .ThenInclude(q => q.Rules)
                .FirstAsync(p => p.Id == quote.ProductId);

            if (!product.IsActive)
            {
                throw new BadRequestException("This product is no longer available.");
            }

            // --- CHANGED: check the saved answers again, against the product's CURRENT questions.
            // The SuperUser may have added a required question since this Draft was created.
            var savedAnswers = JsonSerializer.Deserialize<Dictionary<string, string>>(quote.Answers)
                               ?? new Dictionary<string, string>();

            // The validator expects JSON values, so wrap each saved text answer
            var savedAsJson = savedAnswers.ToDictionary(
                pair => pair.Key,
                pair => JsonSerializer.SerializeToElement(pair.Value));

            var answers = AnswerValidator.Validate(product, savedAsJson);
            // --- END OF CHANGE

            var result = RatingCalculator.Calculate(product, quote.CoverageAmount, answers);

            quote.Premium = result.Premium;
            quote.PremiumBreakdown = JsonSerializer.Serialize(result);
            quote.ExpiresAt = DateTime.UtcNow.AddDays(30);
        }

        quote.Status = newStatus;
        quote.UpdatedAt = DateTime.UtcNow;

        // When the quote is Issued, the policy is created in the same save
        if (action == QuoteAction.Issue)
        {
            await _policyService.CreateFromQuoteAsync(quote);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Quote {QuoteId}: {Action} applied, status is now {Status}", quote.Id, action, quote.Status);

        return ToDto(quote);
    }

    private static bool ExpireIfNeeded(Quote quote)
    {
        var canExpire = quote.Status == QuoteStatus.Quoted || quote.Status == QuoteStatus.Accepted;

        if (canExpire && quote.ExpiresAt.HasValue && quote.ExpiresAt.Value < DateTime.UtcNow)
        {
            quote.Status = QuoteStatus.Expired;
            quote.UpdatedAt = DateTime.UtcNow;
            return true;
        }

        return false;
    }

    private static QuoteDto ToDto(Quote q)
    {
        return new QuoteDto
        {
            Id = q.Id,
            CustomerId = q.CustomerId,
            ProductCode = q.Product!.Code,
            ProductName = q.Product.Name,
            Answers = JsonSerializer.Deserialize<Dictionary<string, string>>(q.Answers)
                      ?? new Dictionary<string, string>(),
            CoverageAmount = q.CoverageAmount,
            Premium = q.Premium,
            PremiumBreakdown = q.PremiumBreakdown == null
                ? null
                : JsonSerializer.Deserialize<PremiumResult>(q.PremiumBreakdown),
            RequestedEffectiveDate = q.RequestedEffectiveDate,
            Status = q.Status,
            CreatedAt = q.CreatedAt,
            ExpiresAt = q.ExpiresAt
        };
    }
}