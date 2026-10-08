using InsuranceApi.Controllers;
using InsuranceApi.Data;
using InsuranceApi.Dtos;
using InsuranceApi.Middleware;
using InsuranceApi.Models;
using InsuranceApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace InsuranceApi.Tests;

public class CustomerServiceTests
{
    // Builds a fresh service with its own empty fake database for every test
    private static CustomerService CreateService()
    {
        var options = new DbContextOptionsBuilder<InsuranceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new InsuranceDbContext(options);
        return new CustomerService(context, NullLogger<CustomerService>.Instance);
    }

    private static CustomerInputDto NewInput(string email = "ana@test.com") => new()
    {
        FirstName = "Ana",
        LastName = "Smith",
        Email = email,
        PhoneNumber = "603-555-0101"
    };

    [Fact] // Test for creating a new customer with valid input
    public async Task CreateAsync_ValidInput_ReturnsCustomerWithId()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.CreateAsync(NewInput());

        // Assert
        Assert.True(result.Id > 0);
        Assert.Equal("Ana", result.FirstName);
        Assert.Equal("ana@test.com", result.Email);
    }

    [Fact]
    public async Task CreateAsync_DuplicateEmail_ThrowsConflictException()
    {
        var service = CreateService();
        await service.CreateAsync(NewInput());

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(NewInput()));
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var service = CreateService();

        var result = await service.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ExistingCustomer_ChangesLastName()
    {
        var service = CreateService();
        var created = await service.CreateAsync(NewInput());

        var update = NewInput();
        update.LastName = "Johnson";
        var result = await service.UpdateAsync(created.Id, update);

        Assert.NotNull(result);
        Assert.Equal("Johnson", result!.LastName);
    }

    [Fact]
    public async Task UpdateAsync_UnknownId_ReturnsNull()
    {
        var service = CreateService();

        var result = await service.UpdateAsync(999, NewInput());

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_ExistingCustomer_ReturnsTrueAndRemovesIt()
    {
        var service = CreateService();
        var created = await service.CreateAsync(NewInput());

        var deleted = await service.DeleteAsync(created.Id);
        var afterDelete = await service.GetByIdAsync(created.Id);

        Assert.True(deleted);
        Assert.Null(afterDelete);
    }

    [Fact]
    public async Task DeleteAsync_UnknownId_ReturnsFalse()
    {
        var service = CreateService();

        var deleted = await service.DeleteAsync(999);

        Assert.False(deleted);
    }
}