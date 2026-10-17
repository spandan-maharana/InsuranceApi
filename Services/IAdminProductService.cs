using InsuranceApi.Dtos;
namespace InsuranceApi.Services;

public interface IAdminProductService
{
    Task<List<AdminProductDtos>> GetAllAsync();
    Task<AdminProductDtos?> GetByCodeAsync(string code);
    Task<AdminProductDtos> CreateAsync(ProductCreateDto input);
    Task<AdminProductDtos?> UpdateAsync(string code, ProductUpdateDto input);
    Task<AdminProductDtos?> SetActiveAsync(string code, bool isActive);
}
