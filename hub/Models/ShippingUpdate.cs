using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace HubApi.Models;

[Table("shipping_updates")]
public class ShippingUpdate
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid OrderId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Provider { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? TrackingNumber { get; set; }
    
    [Column(TypeName = "jsonb")]
    public JsonDocument? Payload { get; set; }
    
    [Required]
    public DateTime OccurredAt { get; set; }
    
    // Navigation properties
    [ForeignKey("OrderId")]
    public virtual Order Order { get; set; } = null!;
}
