using System.Text.Json;
using InsuranceApi.Data;
using InsuranceApi.Dtos;
using InsuranceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace InsuranceApi.Services;

public class QuoteRequestService : IQuoteRequestService
{
    private readonly InsuranceDbContext _context;
    private readonly ILogger<QuoteRequestService> _logger;

    public QuoteRequestService(InsuranceDbContext context, ILogger<QuoteRequestService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<RequestReceivedDto> SubmitAsync(RequestInputDto input)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (input.RequestedEffectiveDate < today)
        {
            throw new BadRequestException("The effective date cannot be in the past.");
        }

        // Find the product, together with its questions
        var code = input.ProductCode.Trim().ToUpperInvariant();
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

        // Check the answers against the product's questions (same checker as quotes)
        var answers = AnswerValidator.Validate(product, input.Answers);

        var request = new QuoteRequest
        {
            FirstName = input.FirstName.Trim(),
            LastName = input.LastName.Trim(),
            Email = input.Email.Trim(),
            PhoneNumber = input.PhoneNumber.Trim(),
            ProductId = product.Id,
            CoverageAmount = input.CoverageAmount,
            RequestedEffectiveDate = input.RequestedEffectiveDate,
            Answers = JsonSerializer.Serialize(answers)
        };

        _context.QuoteRequests.Add(request);
        await _context.SaveChangesAsync();

        // Log the id and the product only, never the person's details
        _logger.LogInformation("Received request {RequestId} for product {ProductCode}", request.Id, product.Code);

        return new RequestReceivedDto
        {
            Id = request.Id,
            Message = "Thank you. Your request has been received and an agent will review it."
        };
    }

    // ----- Agent inbox -----

    public async Task<List<QuoteRequestDto>> ListAsync(RequestStatus? status)
    {
        var query = _context.QuoteRequests.Include(r => r.Product).AsQueryable();

        if (status != null)
        {
            query = query.Where(r => r.Status == status);
        }

        // Oldest first, so the longest-waiting request is at the top
        var requests = await query
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();

        return requests.Select(ToDto).ToList();
    }

    public async Task<QuoteRequestDto?> GetAsync(int id)
    {
        var request = await _context.QuoteRequests
            .Include(r => r.Product)
            .FirstOrDefaultAsync(r => r.Id == id);

        return request == null ? null : ToDto(request);
    }

    public async Task<QuoteRequestDto?> AcceptAsync(int id)
    {
        var request = await _context.QuoteRequests
            .Include(r => r.Product)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (request == null)
        {
            return null;
        }

        if (request.Status != RequestStatus.New)
        {
            throw new ConflictException($"This request has already been {request.Status.ToString().ToLower()}.");
        }

        // Use the real database's transaction so the customer, the quote and the
        // request update all succeed together, or none of them do.
        // (The in-memory test database has no transactions, so we skip it there.)
        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync()
            : null;

        // Reuse the customer if this email is already known
        var email = request.Email.Trim();
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == email);
        if (customer == null)
        {
            customer = new Customer
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = email,
                PhoneNumber = request.PhoneNumber,
                CreatedAt = DateTime.UtcNow
            };
            _context.Customers.Add(customer);
        }

        // Create the Draft quote with the answers the customer gave
        var quote = new Quote
        {
            Customer = customer,
            ProductId = request.ProductId,
            CoverageAmount = request.CoverageAmount,
            RequestedEffectiveDate = request.RequestedEffectiveDate,
            Answers = request.Answers,
            Status = QuoteStatus.Draft
        };
        _context.Quotes.Add(quote);
        await _context.SaveChangesAsync();

        // Now that the customer and quote have ids, record them on the request
        request.Status = RequestStatus.Accepted;
        request.ReviewedAt = DateTime.UtcNow;
        request.CustomerId = customer.Id;
        request.QuoteId = quote.Id;
        await _context.SaveChangesAsync();

        if (transaction != null)
        {
            await transaction.CommitAsync();
        }

        _logger.LogInformation("Accepted request {RequestId}: customer {CustomerId}, quote {QuoteId}",
            request.Id, customer.Id, quote.Id);

        return ToDto(request);
    }

    public async Task<QuoteRequestDto?> DeclineAsync(int id, DeclineRequestDto input)
    {
        var request = await _context.QuoteRequests
            .Include(r => r.Product)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (request == null)
        {
            return null;
        }

        if (request.Status != RequestStatus.New)
        {
            throw new ConflictException($"This request has already been {request.Status.ToString().ToLower()}.");
        }

        request.Status = RequestStatus.Declined;
        request.DeclineReason = input.Reason.Trim();
        request.ReviewedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Declined request {RequestId}", request.Id);

        return ToDto(request);
    }

    // Turns a saved request into what the agent sees
    private static QuoteRequestDto ToDto(QuoteRequest request)
    {
        var answers = JsonSerializer.Deserialize<Dictionary<string, string>>(request.Answers)
                      ?? new Dictionary<string, string>();

        return new QuoteRequestDto
        {
            Id = request.Id,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            ProductCode = request.Product?.Code ?? "",
            ProductName = request.Product?.Name ?? "",
            CoverageAmount = request.CoverageAmount,
            RequestedEffectiveDate = request.RequestedEffectiveDate,
            Answers = answers,
            Status = request.Status,
            DeclineReason = request.DeclineReason,
            CreatedAt = request.CreatedAt,
            ReviewedAt = request.ReviewedAt,
            CustomerId = request.CustomerId,
            QuoteId = request.QuoteId
        };
    }
}