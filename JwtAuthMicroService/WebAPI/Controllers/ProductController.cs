using Application.Services;
using Microsoft.AspNetCore.Mvc;
using WebAPI.ViewModels.Post;

namespace WebAPI.Controllers;

/// <summary>
/// 產品們 <see href="https://dummyjson.com/docs/products">DummyJSON Products - Docs</see>
/// </summary>
public class ProductController: BaseController
{
    private readonly IHostEnvironment _environment;
    private readonly IProductService _service;

    public ProductController(
        IHostEnvironment environment,
        IProductService service)
    {
        _environment = environment;
        _service = service;
    }
    
    /// <summary>
    /// Fetch Any product by ID
    /// </summary>
    [HttpGet("{id}")]
    [Consumes("application/json")]
    public async Task<IActionResult> Get([FromRoute] int id)
    {
        var result = await _service.GetProduct(HttpContext, id);
        
        return Ok(result);
    }
}