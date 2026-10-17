using InsuranceApi.Data;
using InsuranceApi.Dtos;
using Microsoft.EntityFrameworkCore;

namespace InsuranceApi.Services;

public class ProductService : IProductService
{
    private readonly InsuranceDbContext _context;

    public ProductService(InsuranceDbContext context)
    {
        _context = context;
    }

    // Active categories, each with its active products, in the chosen order
    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        var categories = await _context.ProductCategories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .Include(c => c.Products.Where(p => p.IsActive).OrderBy(p => p.SortOrder))
            .ToListAsync();

        return categories.Select(c => new CategoryDto
        {
            Code = c.Code,
            Name = c.Name,
            Products = c.Products.Select(p => new ProductSummaryDto
            {
                Code = p.Code,
                Name = p.Name,
                Description = p.Description
            }).ToList()
        }).ToList();
    }

    // Returns null when the product does not exist or is switched off
    public async Task<ProductDtos?> GetByCodeAsync(string code)
    {
        var normalized = code.Trim().ToUpperInvariant();

        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Questions.OrderBy(q => q.SortOrder))
            .FirstOrDefaultAsync(p => p.Code == normalized && p.IsActive);

        if (product == null)
        {
            return null;
        }

        return new ProductDtos
        {
            Code = product.Code,
            Name = product.Name,
            Description = product.Description,
            CategoryCode = product.Category!.Code,
            CategoryName = product.Category.Name,
            CoverageLabel = product.CoverageLabel,
            MinCoverage = product.MinCoverage,
            MaxCoverage = product.MaxCoverage,
            Questions = product.Questions.Select(q => new QuestionDto
            {
                Key = q.Key,
                Label = q.Label,
                Type = q.Type,
                IsRequired = q.IsRequired,
                MinValue = q.MinValue,
                MaxValue = q.MaxValue,
                Options = SplitOptions(q.Options)
            }).ToList()
        };
    }

    // "Yes,No" in the database becomes ["Yes", "No"]
    private static List<string> SplitOptions(string? options)
    {
        if (string.IsNullOrWhiteSpace(options))
        {
            return new List<string>();
        }

        return options
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

}
