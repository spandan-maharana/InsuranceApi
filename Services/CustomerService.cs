using InsuranceApi.Data;
using InsuranceApi.Dtos;
using InsuranceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace InsuranceApi.Services;

public class CustomerService : ICustomerService
{
    private readonly InsuranceDbContext _context;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(InsuranceDbContext context, ILogger<CustomerService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<CustomerDto>> GetAllAsync()
    {
        var customers = await _context.Customers
            .OrderBy(c => c.LastName)
            .ToListAsync();
        return customers.Select(ToDto).ToList();
    }

    public async Task<CustomerDto?> GetByIdAsync(int id)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id);
        if (customer == null)
        {
            return null;
        }
        return ToDto(customer);
    }

    public async Task<CustomerDto> CreateAsync(CustomerInputDto input)
    {
        // Check for existing email
        var emailExists = await _context.Customers.AnyAsync(c => c.Email == input.Email);
        if (emailExists)
        {
            throw new ConflictException($"A customer with the email '{input.Email}' already exists.");
        }

        var customer = new Customer
        {
            FirstName = input.FirstName,
            LastName = input.LastName,
            Email = input.Email,
            PhoneNumber = input.PhoneNumber
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Created new customer with ID {CustomerId}", customer.Id);

        return ToDto(customer);
    }

    public async Task<CustomerDto?> UpdateAsync(int id, CustomerInputDto input)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id);
        if (customer == null)
        {
            return null;
        }

        var emailExists = await _context.Customers.AnyAsync(c => c.Email == input.Email && c.Id != id);
        if (emailExists)
        {
            throw new ConflictException($"A customer with the email '{input.Email}' already exists.");
        }

        customer.FirstName = input.FirstName;
        customer.LastName = input.LastName;
        customer.Email = input.Email;
        customer.PhoneNumber = input.PhoneNumber;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated customer with ID {CustomerId}", customer.Id);

        return ToDto(customer);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id);
        if (customer == null)
        {
            return false;
        }

        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Deleted customer with ID {CustomerId}", customer.Id);
        return true;
    }

    private static CustomerDto ToDto(Customer c)
    {
        return new CustomerDto
        {
            Id = c.Id,
            FirstName = c.FirstName,
            LastName = c.LastName,
            Email = c.Email,
            PhoneNumber = c.PhoneNumber,
            CreatedAt = c.CreatedAt
        };
    }
}