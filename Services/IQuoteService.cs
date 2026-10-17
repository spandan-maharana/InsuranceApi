using InsuranceApi.Dtos;
using InsuranceApi.Models;

namespace InsuranceApi.Services;

public interface IQuoteService
{
    Task<List<QuoteDto>> GetAllAsync();
    Task<QuoteDto?> GetByIdAsync(int id);
    Task<QuoteDto?> CreateAsync(QuoteInputDto input);
    Task<QuoteDto?> ApplyActionAsync(int id, QuoteAction action);
}
