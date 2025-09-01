using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HubApi.Models;

[Table("order_profits")]
public class OrderProfit
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("order_id")]
    public Guid OrderId { get; set; }

    [Required]
    [Column("site_id")]
    public Guid SiteId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("wc_order_id")]
    public string WcOrderId { get; set; } = string.Empty;

    [Required]
    [Column("order_total", TypeName = "decimal(18,2)")]
    public decimal OrderTotal { get; set; }

    [Required]
    [Column("product_cost", TypeName = "decimal(18,2)")]
    public decimal ProductCost { get; set; }

    [Required]
    [Column("gateway_cost_percentage", TypeName = "decimal(18,2)")]
    public decimal GatewayCostPercentage { get; set; } // Percentage of order total

    [Required]
    [Column("gateway_cost", TypeName = "decimal(18,2)")]
    public decimal GatewayCost { get; set; } // Calculated dollar amount

    [Required]
    [Column("operational_cost", TypeName = "decimal(18,2)")]
    public decimal OperationalCost { get; set; } // Fixed $5.00 per order

    [Column("total_costs", TypeName = "decimal(18,2)")]
    public decimal TotalCosts { get; set; }

    [Column("net_profit", TypeName = "decimal(18,2)")]
    public decimal NetProfit { get; set; }

    [Column("profit_margin", TypeName = "decimal(18,2)")]
    public decimal ProfitMargin { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("payout_status")]
    public string PayoutStatus { get; set; } = "processing"; // "paid", "processing", "refunded"

    [Column("payout_date")]
    public DateTime? PayoutDate { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("is_calculated")]
    public bool IsCalculated { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("OrderId")]
    public virtual OrderV2 Order { get; set; } = null!;

    [ForeignKey("SiteId")]
    public virtual Site Site { get; set; } = null!;
}
