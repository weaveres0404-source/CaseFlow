using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CaseFlow.Server.Models;

[Table("case_logs")]
public partial class CaseLog
{
    [Key]
    [Column("log_id")]
    public int LogId { get; set; }

    [Column("case_id")]
    public int CaseId { get; set; }

    [Column("handler_user_id")]
    public int HandlerUserId { get; set; }

    [Column("log_date")]
    public DateOnly LogDate { get; set; }

    [Column("handling_method")]
    public string HandlingMethod { get; set; } = null!;

    [Column("handling_result")]
    public string? HandlingResult { get; set; }

    [Column("hours_spent")]
    [Precision(5, 2)]
    public decimal HoursSpent { get; set; }

    [Column("headcount")]
    public short Headcount { get; set; }

    [Column("status_after")]
    public short StatusAfter { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("CaseId")]
    [InverseProperty("CaseLogs")]
    public virtual Case Case { get; set; } = null!;

    [ForeignKey("HandlerUserId")]
    [InverseProperty("CaseLogs")]
    public virtual User HandlerUser { get; set; } = null!;
}
