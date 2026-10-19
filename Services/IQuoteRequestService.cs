using InsuranceApi.Dtos;
using InsuranceApi.Models;

namespace InsuranceApi.Services;

public interface IQuoteRequestService
{
    Task<RequestReceivedDto> SubmitAsync(RequestInputDto input);
    Task<List<QuoteRequestDto>> ListAsync(RequestStatus? status);
    Task<QuoteRequestDto?> GetAsync(int id);

    // acceptedBy = the agent's username. It becomes the owner of the new quote.
    Task<QuoteRequestDto?> AcceptAsync(int id, AcceptRequestDto? input = null, string? acceptedBy = null);

    Task<QuoteRequestDto?> DeclineAsync(int id, DeclineRequestDto input);
}