using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HubApi.Models;

[Table("request_nonces")]
public class RequestNonce
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid SiteId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Nonce { get; set; } = string.Empty;
    
    [Required]
    public DateTime Timestamp { get; set; }
    
    [Required]
    public DateTime Expires { get; set; }
    
    // Navigation properties
    [ForeignKey("SiteId")]
    public virtual Site Site { get; set; } = null!;
}
