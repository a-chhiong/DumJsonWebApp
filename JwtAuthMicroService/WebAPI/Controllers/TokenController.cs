using Application.Services;
using JoshAuthorization;
using JoshAuthorization.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using LoginRequest = WebAPI.ViewModels.Login.LoginRequest;

namespace WebAPI.Controllers;

/// <summary>
/// 提供登入/登出/重新驗證機制
/// </summary>
[AllowAnonymous]
public class TokenController: BaseController
{
    private readonly IHostEnvironment _environment;
    private readonly ITokenService _service;

    public TokenController(
        IHostEnvironment environment,
        ITokenService service)
    {
        _environment = environment;
        _service = service;
    }
    
    /// <summary>
    /// 帳號密碼登入
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("Auth")]
    [Consumes("application/json")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _service.Login(Request, request.Email, request.Password);
        
        return Ok(result);
    }
    
    /// <summary>
    /// 更新 Token
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPut("Refresh")]
    [Consumes("application/json")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var result = await _service.Refresh(Request, request.RefreshToken);
        
        return Ok(result);
    }
    
    /// <summary>
    /// 登出
    /// </summary>
    /// <returns></returns>
    [HttpDelete("")]
    public async Task<IActionResult> Logout()
    {
        var result = await _service.Logout(Request);
        
        return Ok(result);
    }
}