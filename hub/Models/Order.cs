using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace HubApi.Models;

[Table("orders")]
public class Order
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid SiteId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string WcOrderId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = string.Empty;
    
    [Required]
    [Column(TypeName = "numeric(12,2)")]
    public decimal OrderTotal { get; set; }
    
    [Required]
    [Column(TypeName = "numeric(12,2)")]
    public decimal Subtotal { get; set; }
    
    [Required]
    [Column(TypeName = "numeric(12,2)")]
    public decimal DiscountTotal { get; set; }
    
    [Required]
    [Column(TypeName = "numeric(12,2)")]
    public decimal ShippingTotal { get; set; }
    
    [Required]
    [Column(TypeName = "numeric(12,2)")]
    public decimal TaxTotal { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string PaymentGatewayCode { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string CustomerName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    [EmailAddress]
    public string CustomerEmail { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string? CustomerPhone { get; set; }
    
    [Column(TypeName = "jsonb")]
    public JsonDocument? ShippingAddress { get; set; }
    
    [Column(TypeName = "jsonb")]
    public JsonDocument? BillingAddress { get; set; }
    
    [Required]
    public DateTime PlacedAt { get; set; }
    
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    [ForeignKey("SiteId")]
    public virtual Site Site { get; set; } = null!;
    
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public virtual ICollection<ShippingUpdate> ShippingUpdates { get; set; } = new List<ShippingUpdate>();
    public virtual ICollection<RevenueShare> RevenueShares { get; set; } = new List<RevenueShare>();
}
