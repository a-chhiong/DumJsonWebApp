using Application.Interfaces;
using CrossCutting.Enums;
using CrossCutting.Extensions;
using CrossCutting.Helpers;

namespace WebAPI.ViewModels;

public class IApiResponse { }

/// <summary>
/// Restful API Response Wrapper
/// </summary>
/// <typeparam name="T"></typeparam>
public class ApiResponse<T> : IApiResponse
{    
    /// <summary>
    /// General Status
    /// </summary>
    public ApiStatusEnum Status { get; set; }
    
    /// <summary>
    /// Custom return code indicating the result of the status
    /// </summary>
    public int? Code { get; set; }
    
    /// <summary>
    /// Custom return name indicating the result of the status
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Human-readable message for frontend or logging
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Human-readable message for frontend or logging
    /// </summary>
    public string? Message { get; set; }
    
    /// <summary>
    /// Payload data (optional, depending on context)
    /// </summary>
    public T? Data { get; set; }
    
    private ApiResponse(ApiStatusEnum status, Enum? code, string? message, T? data)
    {
        Status = status;
        Code = code?.GetInt();
        Name = code?.GetName();
        Description = code?.GetDescription();
        Message = message;
        Data = data;
    }
    
    public static ApiResponse<T> Success(T? data) =>
        new(ApiStatusEnum.Success, null, null, data);
    
    public static ApiResponse<T> Failure(Enum? code, string? message) =>
        new(ApiStatusEnum.Failure, code, message, default);
    
    public static ApiResponse<T> Otherwise(Enum code, string? message, T data) =>
        new(ApiStatusEnum.Otherwise, code, message, data);
}