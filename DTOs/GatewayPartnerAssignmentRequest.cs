using System.ComponentModel.DataAnnotations;

namespace HubApi.DTOs
{
    public class GatewayPartnerAssignmentRequest
    {
        [Required]
        public Guid GatewayPartnerId { get; set; }

        [Required]
        public Guid PaymentGatewayId { get; set; }

        [Required]
        [Range(0, 100)]
        public decimal AssignmentPercentage { get; set; }

        public bool IsActive { get; set; } = true;
    }
}

