using InsuranceApi.Dtos;
using InsuranceApi.Models;

namespace InsuranceApi.Services;

public interface IPolicyService
{
    Task<List<PolicyDto>> GetAllAsync(int? segment);
    Task<PolicyDto?> GetByIdAsync(int id);
    Task<PolicyDto?> GetByQuoteIdAsync(int quoteId);
    Task<Policy> CreateFromQuoteAsync(Quote quote, string? performedBy = null);
    Task<PolicyDto?> RenewAsync(int id, string? performedBy = null);
    Task<PolicyDto?> CancelAsync(int id, string? performedBy = null);
}