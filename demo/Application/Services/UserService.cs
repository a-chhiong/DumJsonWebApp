using Application.Interfaces;
using CrossCutting.Extensions;
using JoshAuthorization.Extensions;
using JoshAuthorization.Models;
using JoshAuthorization.Objects;
using JoshFileCache;
using Microsoft.AspNetCore.Http;

namespace Application.Services;

public interface IUserService
{
    public Task<object> GetMyself(HttpContext context);
}

public class UserService: IUserService
{
    private readonly IDummyJsonAdapter _dummy;

    public UserService(IDummyJsonAdapter dummy)
    {
        _dummy = dummy;
    }
    
    public async Task<object> GetMyself(HttpContext context)
    {        
        var sub = context.GetItem<TokenPayload>()?.sub;
        int.TryParse(sub, out var userId);
        var result = await _dummy.GetUser(userId);
        return result;
    }
}