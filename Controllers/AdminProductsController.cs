using InsuranceApi.Dtos;
using InsuranceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceApi.Controllers;

[ApiController]
[Route("api/admin/products")]
[Authorize(Roles = "SuperUser")]
public class AdminProductsController : ControllerBase
{
    private readonly IAdminProductService _service;

    public AdminProductsController(IAdminProductService service)
    {
        _service = service;
    }

    // GET api/admin/products
    [HttpGet]
    public async Task<ActionResult<List<AdminProductDtos>>> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    // GET api/admin/products/AUTO
    [HttpGet("{code}")]
    public async Task<ActionResult<AdminProductDtos>> GetByCode(string code)
    {
        var product = await _service.GetByCodeAsync(code);
        if (product == null)
        {
            return NotFound(new { error = "Product not found." });
        }
        return Ok(product);
    }

    // POST api/admin/products
    [HttpPost]
    public async Task<ActionResult<AdminProductDtos>> Create(ProductCreateDto input)
    {
        var created = await _service.CreateAsync(input);
        return CreatedAtAction(nameof(GetByCode), new { code = created.Code }, created);
    }

    // PUT api/admin/products/AUTO
    [HttpPut("{code}")]
    public async Task<ActionResult<AdminProductDtos>> Update(string code, ProductUpdateDto input)
    {
        var updated = await _service.UpdateAsync(code, input);
        if (updated == null)
        {
            return NotFound(new { error = "Product not found." });
        }
        return Ok(updated);
    }

    // POST api/admin/products/AUTO/deactivate
    [HttpPost("{code}/deactivate")]
    public async Task<ActionResult<AdminProductDtos>> Deactivate(string code)
    {
        var product = await _service.SetActiveAsync(code, false);
        if (product == null)
        {
            return NotFound(new { error = "Product not found." });
        }
        return Ok(product);
    }

    // POST api/admin/products/AUTO/activate
    [HttpPost("{code}/activate")]
    public async Task<ActionResult<AdminProductDtos>> Activate(string code)
    {
        var product = await _service.SetActiveAsync(code, true);
        if (product == null)
        {
            return NotFound(new { error = "Product not found." });
        }
        return Ok(product);
    }
}