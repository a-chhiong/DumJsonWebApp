using Application.Services;
using Microsoft.AspNetCore.Mvc;
using WebAPI.ViewModels.Post;

namespace WebAPI.Controllers;

/// <summary>
/// 使用者們 <see href="https://dummyjson.com/docs/users">DummyJSON Users - Docs</see>
/// </summary>
public class UserController: BaseController
{
    private readonly IHostEnvironment _environment;
    private readonly IUserService _service;

    public UserController(
        IHostEnvironment environment,
        IUserService service)
    {
        _environment = environment;
        _service = service;
    }
    
    /// <summary>
    /// Fetch Myself
    /// </summary>
    [HttpGet()]
    public async Task<IActionResult> GetMyself()
    {
        var result = await _service.GetMyself(HttpContext);
        
        return Ok(result);
    }
}