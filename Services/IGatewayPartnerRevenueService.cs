using HubApi.Models;

namespace HubApi.Services;

public interface IGatewayPartnerRevenueService
{
    Task<GatewayPartnerRevenueReport> CalculateRevenueAsync(DateTime startDate, DateTime endDate);
    Task<GatewayPartnerRevenueReport> CalculateRevenueForGatewayAsync(Guid gatewayId, DateTime startDate, DateTime endDate);
    Task<GatewayPartnerRevenueReport> CalculateRevenueForPartnerAsync(Guid partnerId, DateTime startDate, DateTime endDate);
}

public class GatewayPartnerRevenueReport
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalRevenue { get; set; }
    public List<GatewayRevenueSummary> GatewaySummaries { get; set; } = new();
    public List<PartnerRevenueSummary> PartnerSummaries { get; set; } = new();
    public List<OrderRevenueDetail> OrderDetails { get; set; } = new();
}

public class GatewayRevenueSummary
{
    public Guid GatewayId { get; set; }
    public string GatewayCode { get; set; } = string.Empty;
    public string GatewayName { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    public decimal TotalFees { get; set; }
    public decimal NetRevenue { get; set; }
    public int OrderCount { get; set; }
}

public class PartnerRevenueSummary
{
    public Guid PartnerId { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public string PartnerCode { get; set; } = string.Empty;
    public decimal TotalRevenueShare { get; set; }
    public int OrderCount { get; set; }
    public List<PartnerGatewayRevenue> GatewayRevenues { get; set; } = new();
}

public class PartnerGatewayRevenue
{
    public Guid GatewayId { get; set; }
    public string GatewayCode { get; set; } = string.Empty;
    public decimal RevenueShare { get; set; }
    public decimal AssignmentPercentage { get; set; }
    public int OrderCount { get; set; }
}

public class OrderRevenueDetail
{
    public Guid OrderId { get; set; }
    public string WcOrderId { get; set; } = string.Empty;
    public string SiteName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal OrderTotal { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime PlacedAt { get; set; }
    public string PaymentGatewayCode { get; set; } = string.Empty;
    public decimal GatewayFees { get; set; }
    public decimal NetRevenue { get; set; }
    public List<OrderPartnerRevenue> PartnerRevenues { get; set; } = new();
}

public class OrderPartnerRevenue
{
    public Guid PartnerId { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public string PartnerCode { get; set; } = string.Empty;
    public decimal RevenueShare { get; set; }
    public decimal AssignmentPercentage { get; set; }
}

