using InsuranceApi.Dtos;
namespace InsuranceApi.Services;

public interface IProductService
{
    Task<List<CategoryDto>> GetCategoriesAsync();
    Task<ProductDtos?> GetByCodeAsync(string code);
}
