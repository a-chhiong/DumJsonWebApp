using Application.Services;
using Microsoft.AspNetCore.Mvc;
using WebApp.ViewModels.Post;

namespace WebApp.Controllers;

/// <summary>
/// 貼文們 <see href="https://dummyjson.com/docs/posts">DummyJSON Posts - Docs</see>
/// </summary>
public class PostController: BaseController
{
    private readonly IHostEnvironment _environment;
    private readonly IPostService _service;

    public PostController(
        IHostEnvironment environment,
        IPostService service)
    {
        _environment = environment;
        _service = service;
    }
    
    /// <summary>
    /// Fetch Any post by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> Get([FromRoute] int id)
    {
        var result = await _service.GetPost(HttpContext, id);
        
        return Ok(result);
    }
    
    /// <summary>
    /// Fetch User's own post
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string keyword)
    {
        var result = await _service.SearchPosts(HttpContext, keyword);
        
        return Ok(result);
    }
    
    /// <summary>
    /// 新增一筆貼文
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost()]
    public async Task<IActionResult> Add([FromBody] PostRequest request)
    {
        var result = await _service.AddPost(HttpContext, request.Title, request.Body);
        
        return Ok(result);
    }
    
    /// <summary>
    /// 更新一筆貼文
    /// </summary>
    /// <param name="id"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] PostRequest request)
    {
        var result = await _service.UpdatePost(id, request.Title, request.Body);
        
        return Ok(result);
    }
    
    /// <summary>
    /// 刪除一筆貼文
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var result = await _service.DeletePost(id);
        
        return Ok(result);
    }
}