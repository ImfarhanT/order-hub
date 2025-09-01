using System.ComponentModel.DataAnnotations;

namespace HubApi.DTOs;

public class OrderProfitRequest
{
    [Required]
    public Guid OrderId { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Product cost must be non-negative")]
    public decimal ProductCost { get; set; }

    [Required]
    [Range(0, 100, ErrorMessage = "Gateway cost percentage must be between 0 and 100")]
    public decimal GatewayCostPercentage { get; set; }

    [Required]
    [MaxLength(20)]
    public string PayoutStatus { get; set; } = "processing"; // "paid", "processing", "refunded"

    // Operational cost is fixed at $5.00 per order
    public decimal OperationalCost { get; set; } = 5.00m;

    public string? Notes { get; set; }
}

public class UpdatePayoutStatusRequest
{
    [Required]
    [MaxLength(20)]
    public string PayoutStatus { get; set; } = "processing"; // "paid", "processing", "refunded"
}
