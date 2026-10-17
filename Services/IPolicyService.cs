using InsuranceApi.Dtos;
using InsuranceApi.Models;

namespace InsuranceApi.Services;

public interface IPolicyService
{
    Task<List<PolicyDto>> GetAllAsync(int? segment);
    Task<PolicyDto?> GetByIdAsync(int id);
    Task<PolicyDto?> GetByQuoteIdAsync(int quoteId);
    Task<Policy> CreateFromQuoteAsync(Quote quote);
    Task<PolicyDto?> RenewAsync(int id);
    Task<PolicyDto?> CancelAsync(int id);
}