using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HubApi.Models;

[Table("partners")]
public class Partner
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(100)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [StringLength(20)]
    [Column("phone")]
    public string? Phone { get; set; }

    [StringLength(100)]
    [Column("company")]
    public string? Company { get; set; }

    [Required]
    [StringLength(20)]
    [Column("share_type")]
    public string ShareType { get; set; } = string.Empty; // "Profit" or "Revenue"

    [Required]
    [Range(0, 100)]
    [Column("share_percentage")]
    public decimal SharePercentage { get; set; }

    [StringLength(500)]
    [Column("address")]
    public string? Address { get; set; }

    [StringLength(1000)]
    [Column("notes")]
    public string? Notes { get; set; }

    [Required]
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<SitePartner> SitePartners { get; set; } = new List<SitePartner>();
    public virtual ICollection<RevenueShare> RevenueShares { get; set; } = new List<RevenueShare>();
    public virtual ICollection<PartnerOrder> PartnerOrders { get; set; } = new List<PartnerOrder>();
}
