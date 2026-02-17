using System.ComponentModel.DataAnnotations;

namespace CrossCutting.Enums;

/// <summary>
/// 整體的API狀態
/// </summary>
public enum ApiStatusEnum
{
    [Display(Name = "Success", Description = "Successful Response")]
    Success = 0,
    
    [Display(Name = "Failure", Description = "Failed Response")]
    Failure = -1,
    
    [Display(Name = "Otherwise", Description = "Neutral Response with extra information to show or action to take")]
    Otherwise = 1
}