using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HubApi.Models;

[Table("revenue_shares")]
public class RevenueShare
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid OrderId { get; set; }
    
    public Guid? PartnerId { get; set; }
    
    [Required]
    [Column(TypeName = "numeric(12,2)")]
    public decimal PartnerShareAmount { get; set; }
    
    [Required]
    [Column(TypeName = "numeric(12,2)")]
    public decimal WebsiteShareAmount { get; set; }
    
    [Required]
    [Column(TypeName = "numeric(12,2)")]
    public decimal GatewayFeeAmount { get; set; }
    
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    [ForeignKey("OrderId")]
    public virtual Order Order { get; set; } = null!;
    
    [ForeignKey("PartnerId")]
    public virtual Partner? Partner { get; set; }
}
