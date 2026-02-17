using System.Net.Http.Json;
using System.Text.Json;
using Application.Interfaces;
using CrossCutting.Logger;
using Domain.ValueObjects.DummyJson;
using Microsoft.Extensions.Hosting.Internal;

namespace Infrastructure.DummyJson;

public class DummyJsonAdapter : IDummyJsonAdapter
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    
    // Use a single, shared instance to prevent socket exhaustion.
    // This is essentially what DI does behind the scenes.
    private static readonly HttpClient _httpClient = new HttpClient 
    { 
        BaseAddress = new Uri("https://dummyjson.com"),
        Timeout = TimeSpan.FromSeconds(30)
    };

    public DummyJsonAdapter() { }

    public async Task<DummyUsers> FetchUser(string query)
    {
        var requestUri = "";
        if (string.IsNullOrEmpty(query))
        {
            requestUri = $"/users";
        }
        else
        {
            requestUri = $"/users/search?q={Uri.EscapeDataString(query)}";
        }
        var response = await _httpClient.GetAsync(requestUri);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Upstream error: {response.StatusCode}, with {query}");

        var json = await response.Content.ReadAsStringAsync();
        var output = JsonSerializer.Deserialize<DummyUsers>(json, _jsonOptions);

        return output ?? throw new ArgumentException("Email is not found!");
    }
    
    public async Task<DummyPosts.Post> GetPost(int id)
    {
        var requestUri = $"/posts/{id}";
        var response = await _httpClient.GetAsync(requestUri);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Upstream error: {response.StatusCode}, with {id}");

        var json = await response.Content.ReadAsStringAsync();
        var output = JsonSerializer.Deserialize<DummyPosts.Post>(json, _jsonOptions);

        return output ?? throw new ArgumentException("Post is not found!");
    }
    
    public async Task<DummyPosts> FetchPosts(string query)
    {
        var requestUri = $"/posts/search?q={Uri.EscapeDataString(query)}";
        var response = await _httpClient.GetAsync(requestUri);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Upstream error: {response.StatusCode}, with {query}");

        var json = await response.Content.ReadAsStringAsync();
        var output = JsonSerializer.Deserialize<DummyPosts>(json, _jsonOptions);

        return output ?? throw new ArgumentException("Post is not found!");
    }

    public async Task<DummyPosts.Post> AddPost(int userId, string title, string body)
    {
        var requestUri = $"/posts/add";
        var response = await _httpClient.PostAsJsonAsync(requestUri, new
        {
            userId = userId,
            title = title,
            body = body,
        });
        
        var json = await response.Content.ReadAsStringAsync();
        var output = JsonSerializer.Deserialize<DummyPosts.Post>(json, _jsonOptions);
        
        return output ?? throw new ArgumentException("Post is not found!");
    }
    
    public async Task<DummyPosts.Post> UpdatePost(int id, string title, string body)
    {
        var requestUri = $"/posts/{id}";
        var response = await _httpClient.PutAsJsonAsync(requestUri, new
        {
            title = title,
            body = body,
        });
        
        var json = await response.Content.ReadAsStringAsync();
        var output = JsonSerializer.Deserialize<DummyPosts.Post>(json, _jsonOptions);
        
        return output ?? throw new ArgumentException("Post is not found!");
    }
    
    public async Task<DummyPosts.Post> DeletePost(int id)
    {
        var requestUri = $"posts/{id}";
        var response = await _httpClient.DeleteAsync(requestUri);
        
        var json = await response.Content.ReadAsStringAsync();
        var output = JsonSerializer.Deserialize<DummyPosts.Post>(json, _jsonOptions);
        
        return output ?? throw new ArgumentException("Post is not found!");
    }
    
    public async Task<DummyProducts.Product> GetProduct(int id)
    {
        var requestUri = $"/products/{id}";
        var response = await _httpClient.GetAsync(requestUri);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Upstream error: {response.StatusCode}, with {id}");

        var json = await response.Content.ReadAsStringAsync();
        var output = JsonSerializer.Deserialize<DummyProducts.Product>(json, _jsonOptions);

        return output ?? throw new ArgumentException("Product is not found!");
    }

    public async Task<DummyProducts> FetchProducts(string query)
    {
        var requestUri = $"/products/search?q={Uri.EscapeDataString(query)}";
        var response = await _httpClient.GetAsync(requestUri);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Upstream error: {response.StatusCode}, with {query}");

        var json = await response.Content.ReadAsStringAsync();
        var output = JsonSerializer.Deserialize<DummyProducts>(json, _jsonOptions);

        return output ?? throw new ArgumentException("Product is not found!");
    }

    public async Task<DummyUsers.User> GetUser(int id)
    {
        var requestUri = $"/users/{id}";
        var response = await _httpClient.GetAsync(requestUri);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Upstream error: {response.StatusCode}, with {id}");

        var json = await response.Content.ReadAsStringAsync();
        var output = JsonSerializer.Deserialize<DummyUsers.User>(json, _jsonOptions);

        return output ?? throw new ArgumentException("User is not found!");
    }
}