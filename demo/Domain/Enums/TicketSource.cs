using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Enums;

/// <summary>
/// 機票資料的來源
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TicketSource
{
    /// <summary>
    /// (原始的)機票
    /// </summary>
    [Display(Name = "`<Tickets>`", Description = "(原始的)機票")]
    Ticket,
    /// <summary>
    /// 候選的機票
    /// </summary>
    [Display(Name = "`<CandidateETickets>`", Description = "候選的機票")]
    CandidateETicket,
    /// <summary>
    /// 過去的機票
    /// </summary>
    [Display(Name = "`<PreviousTickets>`", Description = "過去的機票")]
    PreviousTicket 
}