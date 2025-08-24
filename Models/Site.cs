using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HubApi.Models;

[Table("sites")]
public class Site
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string BaseUrl { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string ApiKey { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string ApiSecretHash { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string ApiSecretEnc { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    public virtual ICollection<SitePartner> SitePartners { get; set; } = new List<SitePartner>();
    public virtual ICollection<SiteGateway> SiteGateways { get; set; } = new List<SiteGateway>();
}
