using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CaseFlow.Server.Models;

[Table("customers")]
public partial class Customer
{
    [Key]
    [Column("customer_id")]
    public int CustomerId { get; set; }

    [Column("customer_name")]
    [StringLength(100)]
    public string CustomerName { get; set; } = null!;

    [Column("contact_person")]
    [StringLength(100)]
    public string? ContactPerson { get; set; }

    [Column("contact_phone")]
    [StringLength(30)]
    public string? ContactPhone { get; set; }

    [Column("contact_email")]
    [StringLength(150)]
    public string? ContactEmail { get; set; }

    [Column("address")]
    [StringLength(255)]
    public string? Address { get; set; }

    [Column("remarks")]
    public string? Remarks { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at", TypeName = "timestamp without time zone")]
    public DateTime UpdatedAt { get; set; }

    [InverseProperty("Customer")]
    public virtual ICollection<Case> Cases { get; set; } = new List<Case>();

    [InverseProperty("Customer")]
    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
}
