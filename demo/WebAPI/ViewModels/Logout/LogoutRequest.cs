using Microsoft.AspNetCore.Identity.Data;

namespace WebAPI.ViewModels.Logout;

public class LogoutRequest
{
    public required string RefreshToken { get; init; }
}