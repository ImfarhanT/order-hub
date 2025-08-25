using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HubApi.Models;

/// <summary>
/// Updated OrderItem model that exactly matches the JSON data structure
/// </summary>
[Table("order_items_v2")]
public class OrderItemV2
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid OrderId { get; set; }
    
    [Required]
    [Column(TypeName = "text")] // Store as text to handle both "158" and 158
    public string ProductId { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? Sku { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [Column(TypeName = "text")] // Store as text to handle both "1" and 1
    public string Qty { get; set; } = string.Empty;
    
    [Required]
    [Column(TypeName = "text")] // Store as text to handle both "89" and 89
    public string Price { get; set; } = string.Empty;
    
    [Required]
    [Column(TypeName = "text")] // Store as text to handle both "89" and 89
    public string Subtotal { get; set; } = string.Empty;
    
    [Required]
    [Column(TypeName = "text")] // Store as text to handle both "89" and 89
    public string Total { get; set; } = string.Empty;
    
    // Navigation properties
    [ForeignKey("OrderId")]
    public virtual OrderV2 Order { get; set; } = null!;
}

