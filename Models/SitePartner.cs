using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HubApi.Models;

[Table("site_partners")]
public class SitePartner
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("site_id")]
    public Guid SiteId { get; set; }

    [Required]
    [Column("partner_id")]
    public Guid PartnerId { get; set; }

    [Required]
    [Column("share_type")]
    public string ShareType { get; set; } = string.Empty; // "Profit" or "Revenue"

    [Required]
    [Range(0, 100)]
    [Column("share_percentage")]
    public decimal SharePercentage { get; set; }

    [Required]
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("notes")]
    public string? Notes { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("SiteId")]
    public virtual Site Site { get; set; } = null!;

    [ForeignKey("PartnerId")]
    public virtual Partner Partner { get; set; } = null!;
}
