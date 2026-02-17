using System.ComponentModel.DataAnnotations;

namespace CrossCutting.Enums;

/// <summary>
/// 錯誤代碼、用於 ApiStatus 爲 -1 的時候
/// </summary>
public enum ErrorCodeEnum
{
    [Display(Name = "Undefined Error", Description = "The Action is not expected.")]
    Undefined = 0,
    
    [Display(Name = "Invalid Request", Description = "The request format or data is invalid.")]
    InvalidRequest = 1001,

    [Display(Name = "Unauthorized", Description = "The user is not authenticated.")]
    Unauthorized = 1002,
    
    [Display(Name = "Server Error", Description = "A server-side error occurred.")]
    ServerError = 2000,
    
    [Display(Name = "Amadeus API Error", Description = "Amadeus Exception occurred.")]
    AmadeusException = 4444,
}