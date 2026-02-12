using Application.Interfaces;
using CrossCutting.Extensions;
using JoshAuthorization.Models;
using JoshFileCache;
using Microsoft.AspNetCore.Http;

namespace Application.Services;

public interface IPostService
{
    public Task<object> GetPost(HttpContext context, int id);
    public Task<object> SearchPosts(HttpContext context, string keywords);
    public Task<object> AddPost(HttpContext context, string title, string body);
    public Task<object> UpdatePost(int id, string title, string body);
    public Task<object> DeletePost(int id);
}

public class PostService: IPostService
{
    private readonly IDummyJsonAdapter _dummy;

    public PostService(IDummyJsonAdapter dummy)
    {
        _dummy = dummy;
    }
    
    public async Task<object> GetPost(HttpContext context, int id)
    {
        var result = await _dummy.GetPost(id);
        return result;
    }
    
    public async Task<object> SearchPosts(HttpContext context, string keywords)
    {
        var meta = context.GetMetadata<JwtMetadata>();
        int.TryParse(meta?.Id, out var userId);
        var result = await _dummy.FetchPosts(keywords);
        return result.posts.Where(p => p.userId == userId).ToList();
    }

    public async Task<object> AddPost(HttpContext context, string title, string body)
    {
        var meta = context.GetMetadata<JwtMetadata>();
        int.TryParse(meta?.Id, out var userId);
        var result = await _dummy.AddPost(userId, title, body);
        return result;
    }
    
    public async Task<object> UpdatePost(int id, string title, string body)
    {
        var result = await _dummy.UpdatePost(id, title, body);
        if (string.IsNullOrEmpty(result.title) || string.IsNullOrEmpty(result.body))
        {
            throw  new ApplicationException("Post Not Found");
        }
        return result;
    }
    
    public async Task<object> DeletePost(int id)
    {
        var result = await _dummy.DeletePost(id);
        if (string.IsNullOrEmpty(result.title) || string.IsNullOrEmpty(result.body))
        {
            throw  new ApplicationException("Post Not Found");
        }
        return result;
    }
}