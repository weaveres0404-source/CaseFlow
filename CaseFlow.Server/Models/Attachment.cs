using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CaseFlow.Server.Models;

[Table("attachments")]
public partial class Attachment
{
    [Key]
    [Column("attachment_id")]
    public int AttachmentId { get; set; }

    [Column("file_name")]
    [StringLength(255)]
    public string FileName { get; set; } = null!;

    [Column("stored_name")]
    [StringLength(255)]
    public string StoredName { get; set; } = null!;

    [Column("file_path")]
    [StringLength(500)]
    public string FilePath { get; set; } = null!;

    [Column("file_size")]
    public int FileSize { get; set; }

    [Column("mime_type")]
    [StringLength(100)]
    public string MimeType { get; set; } = null!;

    [Column("entity_type")]
    [StringLength(50)]
    public string EntityType { get; set; } = null!;

    [Column("entity_id")]
    public int EntityId { get; set; }

    [Column("uploaded_by")]
    public int UploadedBy { get; set; }

    [Column("uploaded_at")]
    public DateTime UploadedAt { get; set; }

    [ForeignKey("UploadedBy")]
    [InverseProperty("Attachments")]
    public virtual User UploadedByNavigation { get; set; } = null!;
}
