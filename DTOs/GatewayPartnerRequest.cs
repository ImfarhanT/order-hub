using System.ComponentModel.DataAnnotations;

namespace HubApi.DTOs
{
    public class GatewayPartnerRequest
    {
        [Required]
        [MaxLength(100)]
        public string PartnerName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string PartnerCode { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [Range(0, 100)]
        public decimal RevenueSharePercentage { get; set; }

        public bool IsActive { get; set; } = true;
    }
}

