using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CaseFlow.Server.Models;

[Table("case_estimations")]
public partial class CaseEstimation
{
    [Key]
    [Column("estimation_id")]
    public int EstimationId { get; set; }

    [Column("case_id")]
    public int CaseId { get; set; }

    [Column("estimator_user_id")]
    public int EstimatorUserId { get; set; }

    [Column("seq_no")]
    public int SeqNo { get; set; }

    [Column("request_date")]
    public DateOnly RequestDate { get; set; }

    [Column("summary")]
    public string Summary { get; set; } = null!;

    [Column("estimated_hours")]
    [Precision(6, 2)]
    public decimal EstimatedHours { get; set; }

    [Column("reply_date")]
    public DateOnly? ReplyDate { get; set; }

    [Column("estimation_status")]
    public short EstimationStatus { get; set; }

    [Column("remarks")]
    public string? Remarks { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("CaseId")]
    [InverseProperty("CaseEstimations")]
    public virtual Case Case { get; set; } = null!;

    [ForeignKey("EstimatorUserId")]
    [InverseProperty("CaseEstimations")]
    public virtual User EstimatorUser { get; set; } = null!;
}
