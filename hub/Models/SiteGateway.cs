using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HubApi.Models;

[Table("site_gateways")]
public class SiteGateway
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid SiteId { get; set; }
    
    [Required]
    public Guid GatewayId { get; set; }
    
    [Required]
    [Column(TypeName = "numeric(5,2)")]
    public decimal WebsiteSharePercent { get; set; }
    
    [MaxLength(1000)]
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    [ForeignKey("SiteId")]
    public virtual Site Site { get; set; } = null!;
    
    [ForeignKey("GatewayId")]
    public virtual PaymentGateway Gateway { get; set; } = null!;
}
