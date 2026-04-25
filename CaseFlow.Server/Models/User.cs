using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CaseFlow.Server.Models;

[Table("users")]
public partial class User
{
    [Key]
    [Column("user_id")]
    public int UserId { get; set; }

    [Column("username")]
    [StringLength(50)]
    public string Username { get; set; } = null!;

    [Column("password_hash")]
    [StringLength(255)]
    public string PasswordHash { get; set; } = null!;

    [Column("full_name")]
    [StringLength(100)]
    public string FullName { get; set; } = null!;

    [Column("email")]
    [StringLength(150)]
    public string Email { get; set; } = null!;

    [Column("phone")]
    [StringLength(30)]
    public string? Phone { get; set; }

    [Column("role")]
    [StringLength(20)]
    public string Role { get; set; } = "SE";

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("last_login_at", TypeName = "timestamp without time zone")]
    public DateTime? LastLoginAt { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at", TypeName = "timestamp without time zone")]
    public DateTime UpdatedAt { get; set; }

    [Column("must_change_password")]
    public bool MustChangePassword { get; set; } = false;

    [InverseProperty("UploadedByNavigation")]
    public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    [InverseProperty("User")]
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    [InverseProperty("AssignedPm")]
    public virtual ICollection<Case> CaseAssignedPms { get; set; } = new List<Case>();

    [InverseProperty("AssignedByNavigation")]
    public virtual ICollection<CaseAssignment> CaseAssignmentAssignedByNavigations { get; set; } = new List<CaseAssignment>();

    [InverseProperty("SeUser")]
    public virtual ICollection<CaseAssignment> CaseAssignmentSeUsers { get; set; } = new List<CaseAssignment>();

    [InverseProperty("CancelledByNavigation")]
    public virtual ICollection<Case> CaseCancelledByNavigations { get; set; } = new List<Case>();

    [InverseProperty("ClosedByNavigation")]
    public virtual ICollection<Case> CaseClosedByNavigations { get; set; } = new List<Case>();

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<Case> CaseCreatedByNavigations { get; set; } = new List<Case>();

    [InverseProperty("EstimatorUser")]
    public virtual ICollection<CaseEstimation> CaseEstimations { get; set; } = new List<CaseEstimation>();

    [InverseProperty("HandlerUser")]
    public virtual ICollection<CaseLog> CaseLogs { get; set; } = new List<CaseLog>();

    [InverseProperty("ReplierUser")]
    public virtual ICollection<CaseReply> CaseReplies { get; set; } = new List<CaseReply>();

    [InverseProperty("RecipientUser")]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    [InverseProperty("User")]
    public virtual ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();
}
