using InsuranceApi.Dtos;

namespace InsuranceApi.Services;

/* Created an Interface (a list of tasks) for the CustomerService class to implement. 
 This interface defines the methods that the CustomerService class must provide, 
ensuring consistency and allowing for easier testing and dependency injection. */

public interface ICustomerService
{
    Task<List<CustomerDto>> GetAllAsync();
    Task<CustomerDto?> GetByIdAsync(int id);
    Task<CustomerDto> CreateAsync(CustomerInputDto customerInputDto);
    Task<CustomerDto?> UpdateAsync(int id, CustomerInputDto input);
    Task<bool> DeleteAsync(int id);
}
