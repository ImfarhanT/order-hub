using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace HubApi.Models;

/// <summary>
/// Updated Order model that exactly matches the JSON data structure
/// </summary>
[Table("orders_v2")]
public class OrderV2
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
    [Column(TypeName = "text")] // Store as text to handle both "98.99" and 89
    public string OrderTotal { get; set; } = string.Empty;
    
    [Required]
    [Column(TypeName = "text")] // Store as text to handle both "89" and 89
    public string Subtotal { get; set; } = string.Empty;
    
    [Required]
    [Column(TypeName = "text")] // Store as text to handle both "0" and 0
    public string DiscountTotal { get; set; } = string.Empty;
    
    [Required]
    [Column(TypeName = "text")] // Store as text to handle both "9.99" and 9.99
    public string ShippingTotal { get; set; } = string.Empty;
    
    [Required]
    [Column(TypeName = "text")] // Store as text to handle both "0" and 0
    public string TaxTotal { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
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
    [Column(TypeName = "text")] // Store as text to handle ISO date strings
    public string PlacedAt { get; set; } = string.Empty;
    
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    [ForeignKey("SiteId")]
    public virtual Site Site { get; set; } = null!;
    
    public virtual ICollection<OrderItemV2> OrderItems { get; set; } = new List<OrderItemV2>();
}

