using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CaseFlow.Server.Models;

[Table("case_replies")]
public partial class CaseReply
{
    [Key]
    [Column("reply_id")]
    public int ReplyId { get; set; }

    [Column("case_id")]
    public int CaseId { get; set; }

    [Column("replier_user_id")]
    public int ReplierUserId { get; set; }

    [Column("reply_date")]
    public DateOnly ReplyDate { get; set; }

    [Column("reply_content")]
    public string ReplyContent { get; set; } = null!;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("CaseId")]
    [InverseProperty("CaseReplies")]
    public virtual Case Case { get; set; } = null!;

    [ForeignKey("ReplierUserId")]
    [InverseProperty("CaseReplies")]
    public virtual User ReplierUser { get; set; } = null!;
}
