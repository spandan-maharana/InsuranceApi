using InsuranceApi.Dtos;
using InsuranceApi.Models;

namespace InsuranceApi.Services;

public interface IQuoteRequestService
{
    Task<RequestReceivedDto> SubmitAsync(RequestInputDto input);

    Task<List<QuoteRequestDto>> ListAsync(RequestStatus? status);
    Task<QuoteRequestDto?> GetAsync(int id);
    Task<QuoteRequestDto?> AcceptAsync(int id);
    Task<QuoteRequestDto?> DeclineAsync(int id, DeclineRequestDto input);
}