using HubApi.Services;

namespace HubApi.Services;

public interface IPdfReportService
{
    byte[] GenerateGatewayRevenuePdf(GatewayRevenueSummary gateway, List<OrderRevenueDetail> orders, DateTime startDate, DateTime endDate);
    byte[] GeneratePartnerRevenuePdf(PartnerRevenueSummary partner, List<OrderRevenueDetail> orders, DateTime startDate, DateTime endDate);
    byte[] GenerateFullRevenuePdf(GatewayPartnerRevenueReport report);
}

