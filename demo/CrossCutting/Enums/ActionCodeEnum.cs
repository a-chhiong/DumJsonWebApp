using System.ComponentModel.DataAnnotations;

namespace CrossCutting.Enums;

/// <summary>
/// 行動代碼、用於 ApiStatus 爲 1 的時候
/// </summary>
public enum ActionCodeEnum
{
    [Display(Name = "Undefined Error", Description = "The Action is not expected.")]
    Undefined = 0,
    
    [Display(Name = "Amadeus API Error", Description = "Amadeus EC Error occurred.")]
    AmadeusErrorCode = 4444,
    
    [Display(Name = "Arguments Error", Description = "Input Arguments Error occurred.")]
    ArgumentsError = 6666,
    
    [Display(Name = "No Data Found", Description = "No Record is matched to the query.")]
    NoDataFound = 7777,
}
