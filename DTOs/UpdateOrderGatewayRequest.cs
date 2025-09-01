using System.ComponentModel.DataAnnotations;

namespace HubApi.DTOs
{
    public class UpdateOrderGatewayRequest
    {
        [Required]
        public string PaymentGatewayCode { get; set; } = string.Empty;
    }
}

