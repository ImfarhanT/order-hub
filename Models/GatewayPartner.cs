using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HubApi.Models
{
    [Table("gateway_partners")]
    public class GatewayPartner
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("partner_name")]
        [MaxLength(100)]
        public string PartnerName { get; set; } = string.Empty;

        [Required]
        [Column("partner_code")]
        [MaxLength(50)]
        public string PartnerCode { get; set; } = string.Empty;

        [Column("description")]
        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [Column("revenue_share_percentage")]
        [Range(0, 100)]
        public decimal RevenueSharePercentage { get; set; }

        [Required]
        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property for gateway assignments
        public virtual ICollection<GatewayPartnerAssignment> GatewayAssignments { get; set; } = new List<GatewayPartnerAssignment>();
    }
}

