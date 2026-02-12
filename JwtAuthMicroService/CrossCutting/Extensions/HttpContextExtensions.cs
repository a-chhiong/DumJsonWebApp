using Microsoft.AspNetCore.Http;

namespace CrossCutting.Extensions;

public static class HttpContextExtensions
{
    // Generic setter
    public static void SetItem<T>(this HttpContext context, T value)
    {
        context.Items[typeof(T)] = value;
    }

    // Generic getter
    public static T? GetItem<T>(this HttpContext context)
    {
        if (context.Items.TryGetValue(typeof(T), out var value) && value is T typed)
        {
            return typed;
        }
        return default;
    }
}