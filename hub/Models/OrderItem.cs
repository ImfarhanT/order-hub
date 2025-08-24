using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HubApi.Models;

[Table("order_items")]
public class OrderItem
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid OrderId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string ProductId { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? Sku { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [Column(TypeName = "numeric(10,2)")]
    public decimal Qty { get; set; }
    
    [Required]
    [Column(TypeName = "numeric(12,2)")]
    public decimal Price { get; set; }
    
    [Required]
    [Column(TypeName = "numeric(12,2)")]
    public decimal Subtotal { get; set; }
    
    [Required]
    [Column(TypeName = "numeric(12,2)")]
    public decimal Total { get; set; }
    
    // Navigation properties
    [ForeignKey("OrderId")]
    public virtual Order Order { get; set; } = null!;
}
