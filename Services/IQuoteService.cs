using InsuranceApi.Dtos;
using InsuranceApi.Models;

namespace InsuranceApi.Services;

public interface IQuoteService
{
    // owner = null returns every quote; otherwise only the quotes created by that user
    Task<List<QuoteDto>> GetAllAsync(string? owner = null);

    Task<QuoteDto?> GetByIdAsync(int id);

    // Returns null when the customer does not exist
    Task<QuoteDto?> CreateAsync(QuoteInputDto input, string? createdBy = null);

    Task<QuoteDto?> ApplyActionAsync(int id, QuoteAction action);
}