using Application.Interfaces;
using Application.Services;
using Infrastructure.DummyJson;
using JoshAuthorization;

namespace WebApp.ServiceCollection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationValidators(this IServiceCollection services)
    {
        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IUserService, UserService>();
        return services;
    }
    
    public static IServiceCollection AddJwtAuthService(this IServiceCollection services)
    {
        // Register JwtAuthService as a singleton
        services.AddSingleton<IJwtAuthService, JwtAuthService>();
        
        return services;
    }

    public static IServiceCollection AddAdapterService(this IServiceCollection services)
    {
        services.AddScoped<IDummyJsonAdapter, DummyJsonAdapter>();
        return services;
    }
}
