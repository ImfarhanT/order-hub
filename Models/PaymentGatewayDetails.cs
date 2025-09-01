using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HubApi.Models;

[Table("payment_gateway_details")]
public class PaymentGatewayDetails
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("gateway_code")]
    [MaxLength(100)]
    public string GatewayCode { get; set; } = string.Empty;

    [Column("descriptor")]
    [MaxLength(500)]
    public string? Descriptor { get; set; }

    [Column("fees_percentage", TypeName = "decimal(5,2)")]
    [Range(0, 100, ErrorMessage = "Fees percentage must be between 0 and 100")]
    public decimal? FeesPercentage { get; set; }

    [Column("fees_fixed", TypeName = "decimal(18,2)")]
    public decimal? FeesFixed { get; set; }

    [Column("fee_type")]
    [MaxLength(20)]
    public string FeeType { get; set; } = "percentage"; // "percentage" or "fixed"
}
