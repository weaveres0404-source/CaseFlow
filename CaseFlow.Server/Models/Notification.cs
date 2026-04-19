using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CaseFlow.Server.Models;

[Table("notifications")]
public partial class Notification
{
    [Key]
    [Column("notification_id")]
    public int NotificationId { get; set; }

    [Column("recipient_user_id")]
    public int RecipientUserId { get; set; }

    [Column("case_id")]
    public int? CaseId { get; set; }

    [Column("notification_type")]
    [StringLength(50)]
    public string NotificationType { get; set; } = null!;

    [Column("title")]
    [StringLength(200)]
    public string Title { get; set; } = null!;

    [Column("message")]
    public string Message { get; set; } = null!;

    [Column("is_read")]
    public bool IsRead { get; set; }

    [Column("read_at")]
    public DateTime? ReadAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("CaseId")]
    [InverseProperty("Notifications")]
    public virtual Case? Case { get; set; }

    [ForeignKey("RecipientUserId")]
    [InverseProperty("Notifications")]
    public virtual User RecipientUser { get; set; } = null!;
}
