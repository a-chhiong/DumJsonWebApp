using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace CrossCutting.Helpers;

public enum TaiwanIdentityType
{
    [Display(Name = "12", Description = "臺灣身分證")]
    Citizens,   //臺灣身分證
    [Display(Name = "89", Description = "臺灣永久居留證")]
    Residents,  //永久居留證
    [Display(Name = "-", Description = "非臺灣證件")]
    None        //皆非
}

public static class TaiwanIdentityHelper
{
    public static TaiwanIdentityType Examine(string number)
    {
        if (IsStartWithUppercaseLetter(number) && IsValidChecksum(number))
        {
            if (isCitizenFormat(number)) return TaiwanIdentityType.Citizens;
            if (isResidentFormat(number)) return TaiwanIdentityType.Residents;
        }
        
        return TaiwanIdentityType.None;
    }

    public static bool IsStartWithUppercaseLetter(string number)
    {
        return char.IsLetter(number[0]) && char.IsUpper(number[0]);
    }
    
    public static bool IsRemainByNumber(string number)
    {
        return number.Skip(2).All(char.IsDigit);
    }

    public static bool isCitizenFormat(string number)
    {
        return "12".IndexOf(number[1]) != -1;
    }
    
    public static bool isResidentFormat(string number)
    {
        return "89".IndexOf(number[1]) != -1;
    }
    
    public static bool IsValidChecksum(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return false;

        id = id.Trim();

        // Regex: first letter, then 1 - 9, then 8 digits
        var regex = new Regex("^[A-Z][1-9]\\d{8}$");
        if (!regex.IsMatch(id))
            return false;

        string conver = "ABCDEFGHJKLMNPQRSTUVXYWZIO";
        int[] weights = { 1, 9, 8, 7, 6, 5, 4, 3, 2, 1, 1 };

        // Replace first letter with its numeric code (index + 10)
        int index = conver.IndexOf(id[0]);
        if (index < 0) return false;

        id = (index + 10).ToString() + id.Substring(1);

        int checkSum = 0;
        for (int i = 0; i < id.Length; i++)
        {
            int c = int.Parse(id[i].ToString());
            int w = weights[i];
            checkSum += c * w;
        }

        return checkSum % 10 == 0;
    }
}