using System.ComponentModel.DataAnnotations;

namespace HubApi.DTOs;

public class SitePartnerRequest
{
    [Required]
    public Guid siteId { get; set; }

    [Required]
    public Guid partnerId { get; set; }

    [Required]
    [StringLength(20)]
    public string shareType { get; set; } = string.Empty; // "Profit" or "Revenue"

    [Required]
    [Range(0, 100)]
    public decimal sharePercentage { get; set; }

    public bool isActive { get; set; } = true;

    public string? Notes { get; set; }
}
