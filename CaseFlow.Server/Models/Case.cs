using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CaseFlow.Server.Models;

[Table("cases")]
[Index("CaseNumber", Name = "cases_case_number_key", IsUnique = true)]
public partial class Case
{
    [Key]
    [Column("case_id")]
    public int CaseId { get; set; }

    [Column("case_number")]
    [StringLength(30)]
    public string CaseNumber { get; set; } = null!;

    [Column("project_id")]
    public int ProjectId { get; set; }

    [Column("customer_id")]
    public int CustomerId { get; set; }

    [Column("category_id")]
    public int CategoryId { get; set; }

    [Column("module_id")]
    public int? ModuleId { get; set; }

    [Column("reporter_name")]
    [StringLength(100)]
    public string ReporterName { get; set; } = null!;

    [Column("reporter_phone")]
    [StringLength(30)]
    public string? ReporterPhone { get; set; }

    [Column("reporter_email")]
    [StringLength(150)]
    public string? ReporterEmail { get; set; }

    [Column("case_type")]
    [StringLength(20)]
    public string CaseType { get; set; } = null!;

    [Column("priority")]
    [StringLength(10)]
    public string Priority { get; set; } = null!;

    [Column("description")]
    public string Description { get; set; } = null!;

    [Column("status")]
    public short Status { get; set; }

    [Column("created_by")]
    public int CreatedBy { get; set; }

    [Column("assigned_pm_id")]
    public int? AssignedPmId { get; set; }

    [Column("closed_by")]
    public int? ClosedBy { get; set; }

    [Column("cancelled_by")]
    public int? CancelledBy { get; set; }

    [Column("related_case_id")]
    public int? RelatedCaseId { get; set; }

    [Column("relation_type")]
    [StringLength(20)]
    public string? RelationType { get; set; }

    [Column("closed_at")]
    public DateTime? ClosedAt { get; set; }

    [Column("cancelled_at")]
    public DateTime? CancelledAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    /// <summary>累計工時（小時），不寫入 DB，由 API 透過 CaseLog.Sum 動態計算</summary>
    [NotMapped]
    public decimal TotalHours { get; set; }

    [ForeignKey("AssignedPmId")]
    [InverseProperty("CaseAssignedPms")]
    public virtual User? AssignedPm { get; set; }

    [InverseProperty("Case")]
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    [ForeignKey("CancelledBy")]
    [InverseProperty("CaseCancelledByNavigations")]
    public virtual User? CancelledByNavigation { get; set; }

    [InverseProperty("Case")]
    public virtual ICollection<CaseAssignment> CaseAssignments { get; set; } = new List<CaseAssignment>();

    [InverseProperty("Case")]
    public virtual ICollection<CaseEstimation> CaseEstimations { get; set; } = new List<CaseEstimation>();

    [InverseProperty("Case")]
    public virtual ICollection<CaseLog> CaseLogs { get; set; } = new List<CaseLog>();

    [InverseProperty("Case")]
    public virtual ICollection<CaseReply> CaseReplies { get; set; } = new List<CaseReply>();

    [ForeignKey("CategoryId")]
    [InverseProperty("Cases")]
    public virtual ProblemCategory Category { get; set; } = null!;

    [ForeignKey("ClosedBy")]
    [InverseProperty("CaseClosedByNavigations")]
    public virtual User? ClosedByNavigation { get; set; }

    [ForeignKey("CreatedBy")]
    [InverseProperty("CaseCreatedByNavigations")]
    public virtual User CreatedByNavigation { get; set; } = null!;

    [ForeignKey("CustomerId")]
    [InverseProperty("Cases")]
    public virtual Customer Customer { get; set; } = null!;

    [InverseProperty("RelatedCase")]
    public virtual ICollection<Case> InverseRelatedCase { get; set; } = new List<Case>();

    [ForeignKey("ModuleId")]
    [InverseProperty("Cases")]
    public virtual SystemModule? Module { get; set; }

    [InverseProperty("Case")]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    [ForeignKey("ProjectId")]
    [InverseProperty("Cases")]
    public virtual Project Project { get; set; } = null!;

    [ForeignKey("RelatedCaseId")]
    [InverseProperty("InverseRelatedCase")]
    public virtual Case? RelatedCase { get; set; }
}
