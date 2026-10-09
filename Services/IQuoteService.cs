using InsuranceApi.Dtos;

namespace InsuranceApi.Services;

public interface IQuoteService
{
    Task<List<QuoteDto>> GetAllAsync();
    Task<QuoteDto?> GetByIdAsync(int id);
    Task<QuoteDto?> CreateAsync(QuoteInputDto input);
}
