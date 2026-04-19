using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CaseFlow.Server.Models;

[Table("case_assignments")]
public partial class CaseAssignment
{
    [Key]
    [Column("assignment_id")]
    public int AssignmentId { get; set; }

    [Column("case_id")]
    public int CaseId { get; set; }

    [Column("se_user_id")]
    public int SeUserId { get; set; }

    [Column("assigned_by")]
    public int AssignedBy { get; set; }

    [Column("is_primary")]
    public bool IsPrimary { get; set; }

    [Column("instructions")]
    public string? Instructions { get; set; }

    [Column("expected_completion_date")]
    public DateOnly? ExpectedCompletionDate { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("assigned_at")]
    public DateTime AssignedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("AssignedBy")]
    [InverseProperty("CaseAssignmentAssignedByNavigations")]
    public virtual User AssignedByNavigation { get; set; } = null!;

    [ForeignKey("CaseId")]
    [InverseProperty("CaseAssignments")]
    public virtual Case Case { get; set; } = null!;

    [ForeignKey("SeUserId")]
    [InverseProperty("CaseAssignmentSeUsers")]
    public virtual User SeUser { get; set; } = null!;
}
