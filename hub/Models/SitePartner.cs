using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HubApi.Models;

[Table("site_partners")]
public class SitePartner
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid SiteId { get; set; }
    
    [Required]
    public Guid PartnerId { get; set; }
    
    [Required]
    [Column(TypeName = "numeric(5,2)")]
    public decimal SharePercent { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    [ForeignKey("SiteId")]
    public virtual Site Site { get; set; } = null!;
    
    [ForeignKey("PartnerId")]
    public virtual Partner Partner { get; set; } = null!;
}
