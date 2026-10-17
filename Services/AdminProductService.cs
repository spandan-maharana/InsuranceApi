using InsuranceApi.Data;
using InsuranceApi.Dtos;
using InsuranceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace InsuranceApi.Services;

public class AdminProductService : IAdminProductService
{
    private readonly InsuranceDbContext _context;
    private readonly ILogger<AdminProductService> _logger;

    public AdminProductService(InsuranceDbContext context, ILogger<AdminProductService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Admins see inactive products too
    public async Task<List<AdminProductDtos>> GetAllAsync()
    {
        var products = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Questions)
            .OrderBy(p => p.Category!.SortOrder)
            .ThenBy(p => p.SortOrder)
            .ToListAsync();

        return products.Select(ToDto).ToList();
    }

    public async Task<AdminProductDtos?> GetByCodeAsync(string code)
    {
        var product = await LoadAsync(code, track: false);
        return product == null ? null : ToDto(product);
    }

    public async Task<AdminProductDtos> CreateAsync(ProductCreateDto input)
    {
        var code = input.Code!.Trim().ToUpperInvariant();

        var exists = await _context.Products.AnyAsync(p => p.Code == code);
        if (exists)
        {
            throw new ConflictException($"A product with the code '{code}' already exists.");
        }

        CheckCoverageRange(input.MinCoverage, input.MaxCoverage);
        var category = await FindCategoryAsync(input.CategoryCode!);

        var product = new Product
        {
            Code = code,
            CategoryId = category.Id,
            Category = category,
            Name = input.Name!.Trim(),
            Description = input.Description?.Trim(),
            CoverageLabel = input.CoverageLabel!.Trim(),
            RatePerThousand = input.RatePerThousand,
            MinimumPremium = input.MinimumPremium,
            PolicyFee = input.PolicyFee,
            MinCoverage = input.MinCoverage,
            MaxCoverage = input.MaxCoverage,
            SortOrder = input.SortOrder,
            IsActive = true
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Product {Code} created", product.Code);

        return ToDto(product);
    }

    public async Task<AdminProductDtos?> UpdateAsync(string code, ProductUpdateDto input)
    {
        var product = await LoadAsync(code, track: true);
        if (product == null)
        {
            return null;
        }

        CheckCoverageRange(input.MinCoverage, input.MaxCoverage);
        var category = await FindCategoryAsync(input.CategoryCode!);

        product.CategoryId = category.Id;
        product.Category = category;
        product.Name = input.Name!.Trim();
        product.Description = input.Description?.Trim();
        product.CoverageLabel = input.CoverageLabel!.Trim();
        product.RatePerThousand = input.RatePerThousand;
        product.MinimumPremium = input.MinimumPremium;
        product.PolicyFee = input.PolicyFee;
        product.MinCoverage = input.MinCoverage;
        product.MaxCoverage = input.MaxCoverage;
        product.SortOrder = input.SortOrder;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Product {Code} updated", product.Code);

        return ToDto(product);
    }

    public async Task<AdminProductDtos?> SetActiveAsync(string code, bool isActive)
    {
        var product = await LoadAsync(code, track: true);
        if (product == null)
        {
            return null;
        }

        product.IsActive = isActive;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Product {Code} active = {IsActive}", product.Code, isActive);

        return ToDto(product);
    }

    private async Task<Product?> LoadAsync(string code, bool track)
    {
        var clean = code.Trim().ToUpperInvariant();

        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Questions)
            .AsQueryable();

        if (!track)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(p => p.Code == clean);
    }

    private async Task<ProductCategory> FindCategoryAsync(string categoryCode)
    {
        var clean = categoryCode.Trim().ToUpperInvariant();
        var category = await _context.ProductCategories.FirstOrDefaultAsync(c => c.Code == clean);
        if (category == null)
        {
            throw new BadRequestException($"Unknown category '{categoryCode}'.");
        }
        return category;
    }

    private static void CheckCoverageRange(decimal min, decimal max)
    {
        if (min > max)
        {
            throw new BadRequestException("Minimum coverage cannot be bigger than maximum coverage.");
        }
    }

    private static AdminProductDtos ToDto(Product p)
    {
        return new AdminProductDtos
        {
            Code = p.Code,
            Name = p.Name,
            Description = p.Description,
            CategoryCode = p.Category!.Code,
            CategoryName = p.Category.Name,
            CoverageLabel = p.CoverageLabel,
            RatePerThousand = p.RatePerThousand,
            MinimumPremium = p.MinimumPremium,
            PolicyFee = p.PolicyFee,
            MinCoverage = p.MinCoverage,
            MaxCoverage = p.MaxCoverage,
            SortOrder = p.SortOrder,
            IsActive = p.IsActive,
            QuestionCount = p.Questions.Count
        };
    }
}