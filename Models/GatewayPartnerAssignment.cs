using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HubApi.Models
{
    [Table("gateway_partner_assignments")]
    public class GatewayPartnerAssignment
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("gateway_partner_id")]
        public Guid GatewayPartnerId { get; set; }

        [Required]
        [Column("payment_gateway_id")]
        public Guid PaymentGatewayId { get; set; }

        [Required]
        [Column("assignment_percentage")]
        [Range(0, 100)]
        public decimal AssignmentPercentage { get; set; }

        [Required]
        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("GatewayPartnerId")]
        public virtual GatewayPartner GatewayPartner { get; set; } = null!;

        [ForeignKey("PaymentGatewayId")]
        public virtual PaymentGatewayDetails PaymentGateway { get; set; } = null!;
    }
}

