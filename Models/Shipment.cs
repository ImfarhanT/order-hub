using System.ComponentModel.DataAnnotations;

namespace HubApi.Models;

public class Shipment
{
    public Guid Id { get; set; }
    
    [Required]
    public Guid OrderId { get; set; }
    
    [Required]
    public string TrackingNumber { get; set; } = string.Empty;
    
    [Required]
    public string Carrier { get; set; } = string.Empty; // FedEx, UPS, DHL, etc.
    
    [Required]
    public string Status { get; set; } = "pending"; // pending, shipped, delivered, exception
    
    public string? TrackingUrl { get; set; }
    
    public DateTime? ShippedAt { get; set; }
    
    public DateTime? EstimatedDelivery { get; set; }
    
    public DateTime? DeliveredAt { get; set; }
    
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual OrderV2 Order { get; set; } = null!;
}
