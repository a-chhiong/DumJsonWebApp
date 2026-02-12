using Application.Interfaces;
using CrossCutting.Extensions;
using JoshAuthorization.Models;
using JoshFileCache;
using Microsoft.AspNetCore.Http;

namespace Application.Services;

public interface IProductService
{
    public Task<object> GetProduct(HttpContext context, int id);
}

public class ProductService: IProductService
{
    private readonly IDummyJsonAdapter _dummy;

    public ProductService(IDummyJsonAdapter dummy)
    {
        _dummy = dummy;
    }
    
    public async Task<object> GetProduct(HttpContext context, int id)
    {
        var result = await _dummy.GetProduct(id);
        return result;
    }
}