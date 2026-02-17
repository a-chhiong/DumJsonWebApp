using Domain.ValueObjects.DummyJson;

namespace Application.Interfaces;

public interface IDummyJsonAdapter
{
    Task<DummyUsers> FetchUser(string query);
    Task<DummyPosts.Post> GetPost(int id);
    Task<DummyPosts> FetchPosts(string query);
    Task<DummyPosts.Post> AddPost(int userId, string title, string body);
    Task<DummyPosts.Post> UpdatePost(int id, string title, string body);
    Task<DummyPosts.Post> DeletePost(int id);
    Task<DummyProducts.Product> GetProduct(int id);
    Task<DummyProducts> FetchProducts(string query);
    Task<DummyUsers.User> GetUser(int id);
}