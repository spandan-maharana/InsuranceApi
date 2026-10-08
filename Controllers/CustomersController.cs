using Microsoft.AspNetCore.Authorization;
using InsuranceApi.Dtos;
using InsuranceApi.Models;
using InsuranceApi.Services;
using Microsoft.AspNetCore.Mvc;


namespace InsuranceApi.Controllers;

[ApiController] //Invoked the ApiController attribute to enable automatic model validation and other features for the controller.
[Route("api/[controller]")] //Defines the route for the controller, where [controller] is replaced with the name of the controller (in this case, "customers") [/api/customers].
[Authorize] //Requires authentication for all actions in the controller.
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _service;
    
    public CustomersController(ICustomerService service) //Constructor that takes an ICustomerService instance as a parameter. This allows for dependency injection of the service, enabling the controller to use the service's methods for handling customer-related operations.
    {
        _service = service;
    }

    [HttpGet] //Defines an HTTP GET endpoint for retrieving all customers. When a GET request is made to /api/customers, this method will be invoked.
    public async Task<ActionResult<List<CustomerDto>>> GetAll()
    {
        var customers = await _service.GetAllAsync();
        return Ok(customers);
    }

    [HttpGet("{id}")] //Defines an HTTP GET endpoint for retrieving a specific customer by ID. When a GET request is made to /api/customers/{id}, this method will be invoked, where {id} is a placeholder for the customer ID.
    public async Task<ActionResult<CustomerDto>> GetById(int id)
    {
        var customer = await _service.GetByIdAsync(id);
        if (customer == null)
        {
            //HTTP 404 Not Found response if the customer with the specified ID is not found in the list.
            return NotFound();
        }
        return Ok(customer);
    }

    [HttpPost] //Defines an HTTP POST endpoint for creating a new customer. When a POST request is made to /api/customers, this method will be invoked.
    public async Task<ActionResult<CustomerDto>> Create(CustomerInputDto input)
    {
        var created = await _service.CreateAsync(input);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")] //Defines an HTTP PUT endpoint for updating an existing customer by ID. When a PUT request is made to /api/customers/{id}, this method will be invoked, where {id} is a placeholder for the customer ID.
    public async Task<ActionResult<CustomerDto>> Update(int id, CustomerInputDto input)
    {
        var updated = await _service.UpdateAsync(id, input);
        if (updated == null)
        {
            return NotFound();
        }
        return Ok(updated);
    }

    [Authorize(Roles = "SuperUser")] //Restricts access to this endpoint to users with the "SuperUser" role. Only authenticated users with this role can invoke the Delete method.
    [HttpDelete("{id}")] //Defines an HTTP DELETE endpoint for deleting a customer by ID. When a DELETE request is made to /api/customers/{id}, this method will be invoked, where {id} is a placeholder for the customer ID.
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound();
        }
        return NoContent();
    }
}


/* Status Codes:
 * 200 OK: The request was successful, and the response contains the requested data.
 * 201 Created: The request was successful, and a new resource was created as a result. This is typically used in response to POST requests.
 * 204 No Content: The request was successful, but there is no content to return. This is often used in response to DELETE requests.
 * 404 Not Found: The requested resource could not be found. This is typically used when a resource with the specified ID does not exist.
 */
