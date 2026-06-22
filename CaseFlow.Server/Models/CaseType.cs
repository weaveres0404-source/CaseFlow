using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CaseFlow.Server.Models;

[Table("case_types")]
[Index("Code", Name = "uk_case_types_code", IsUnique = true)]
public partial class CaseType
{
    [Key]
    [Column("type_id")]
    public int TypeId { get; set; }

    [Column("code")]
    [StringLength(20)]
    public string Code { get; set; } = null!;

    [Column("label")]
    [StringLength(100)]
    public string Label { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("color")]
    [StringLength(50)]
    public string? Color { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at", TypeName = "timestamp without time zone")]
    public DateTime UpdatedAt { get; set; }

    [InverseProperty("CaseType")]
    public virtual ICollection<CaseTypeProject> ProjectLinks { get; set; } = new List<CaseTypeProject>();

    [InverseProperty("CaseType")]
    public virtual ICollection<ProblemCategoryCaseType> CategoryLinks { get; set; } = new List<ProblemCategoryCaseType>();
}
