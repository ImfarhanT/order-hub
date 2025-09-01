using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HubApi.Models;

[Table("partner_orders")]
public class PartnerOrder
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("partner_id")]
    public Guid PartnerId { get; set; }

    [Required]
    [Column("order_id")]
    public Guid OrderId { get; set; }

    [Required]
    [Column("order_total")]
    public decimal OrderTotal { get; set; }

    [Required]
    [Column("share_amount")]
    public decimal ShareAmount { get; set; }

    [Required]
    [Column("share_type")]
    public string ShareType { get; set; } = string.Empty; // "Profit" or "Revenue"

    [Required]
    [Column("share_percentage")]
    public decimal SharePercentage { get; set; }

    [Required]
    [Column("is_paid")]
    public bool IsPaid { get; set; } = false;

    [Column("paid_at")]
    public DateTime? PaidAt { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("PartnerId")]
    public virtual Partner Partner { get; set; } = null!;

    [ForeignKey("OrderId")]
    public virtual OrderV2 Order { get; set; } = null!;
}
