using HubApi.Data;
using HubApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HubApi.Services;

public class GatewayPartnerRevenueService : IGatewayPartnerRevenueService
{
    private readonly OrderHubDbContext _context;
    private readonly ILogger<GatewayPartnerRevenueService> _logger;

    public GatewayPartnerRevenueService(OrderHubDbContext context, ILogger<GatewayPartnerRevenueService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<GatewayPartnerRevenueReport> CalculateRevenueAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            _logger.LogInformation("Calculating revenue for period {StartDate} to {EndDate}", startDate, endDate);

            var report = new GatewayPartnerRevenueReport
            {
                StartDate = startDate,
                EndDate = endDate
            };

            // Get total order count for monitoring
            var totalOrderCount = await GetTotalOrderCountAsync();
            _logger.LogInformation("Total orders in database: {TotalOrderCount}", totalOrderCount);

            // Get orders in the date range using the helper method
            var allOrders = await GetOrdersInDateRangeAsync(startDate, endDate);
            
            _logger.LogInformation("Total orders found in date range: {TotalOrders}", allOrders.Count);
            
            // Log gateway distribution
            var gatewayDistribution = allOrders
                .Where(o => !string.IsNullOrEmpty(o.PaymentGatewayCode))
                .GroupBy(o => o.PaymentGatewayCode)
                .Select(g => new { Gateway = g.Key, Count = g.Count() })
                .ToList();
            
            foreach (var gateway in gatewayDistribution)
            {
                _logger.LogInformation("Gateway {Gateway}: {Count} orders", gateway.Gateway, gateway.Count);
            }

            // Log orders without payment gateway codes
            var ordersWithoutGateway = allOrders.Where(o => string.IsNullOrEmpty(o.PaymentGatewayCode)).ToList();
            if (ordersWithoutGateway.Any())
            {
                _logger.LogWarning("Found {Count} orders without payment gateway codes", ordersWithoutGateway.Count);
                foreach (var order in ordersWithoutGateway.Take(5)) // Log first 5
                {
                    _logger.LogWarning("Order {OrderId} has no payment gateway code", order.Id);
                }
            }

            // Get all gateway partner assignments
            var assignments = await _context.GatewayPartnerAssignments
                .Include(ga => ga.GatewayPartner)
                .Include(ga => ga.PaymentGateway)
                .Where(ga => ga.IsActive)
                .ToListAsync();

            // Get all payment gateway details for fee calculations
            var gatewayDetails = await _context.PaymentGatewayDetails.ToListAsync();
            
            _logger.LogInformation("Found {GatewayCount} payment gateways and {AssignmentCount} active assignments", 
                gatewayDetails.Count, assignments.Count);

            // Calculate revenue for each order
            var processedOrders = 0;
            var successfulOrders = 0;
            var skippedOrders = 0;
            
            foreach (var order in allOrders)
            {
                processedOrders++;
                var orderRevenue = await CalculateOrderRevenue(order, assignments, gatewayDetails);
                if (orderRevenue != null)
                {
                    report.OrderDetails.Add(orderRevenue);
                    report.TotalRevenue += orderRevenue.OrderTotal;
                    successfulOrders++;
                }
                else
                {
                    // Try to create a basic order revenue without partner assignments
                    var basicOrderRevenue = await CreateBasicOrderRevenue(order, gatewayDetails);
                    if (basicOrderRevenue != null)
                    {
                        report.OrderDetails.Add(basicOrderRevenue);
                        report.TotalRevenue += basicOrderRevenue.OrderTotal;
                        successfulOrders++;
                        _logger.LogInformation("Order {OrderId} included with basic revenue calculation (no partner assignments)", order.Id);
                    }
                    else
                    {
                        skippedOrders++;
                        _logger.LogWarning("Order {OrderId} with gateway {Gateway} was skipped - likely missing gateway details", 
                            order.Id, order.PaymentGatewayCode);
                    }
                }
            }
            
            _logger.LogInformation("Orders processed: {Processed}, Orders with revenue calculated: {Successful}, Orders skipped: {Skipped}", 
                processedOrders, successfulOrders, skippedOrders);

            // Generate gateway summaries
            report.GatewaySummaries = await GenerateGatewaySummaries(report.OrderDetails, gatewayDetails);

            // Generate partner summaries
            report.PartnerSummaries = await GeneratePartnerSummaries(report.OrderDetails, assignments);

            _logger.LogInformation("Revenue calculation completed. Total revenue: {TotalRevenue}, Orders: {OrderCount}", 
                report.TotalRevenue, report.OrderDetails.Count);

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating revenue for period {StartDate} to {EndDate}", startDate, endDate);
            throw;
        }
    }

    public async Task<GatewayPartnerRevenueReport> CalculateRevenueForGatewayAsync(Guid gatewayId, DateTime startDate, DateTime endDate)
    {
        try
        {
            // Get gateway details to find the gateway code
            var gatewayDetails = await _context.PaymentGatewayDetails.ToListAsync();
            var gateway = gatewayDetails.FirstOrDefault(g => g.Id == gatewayId);
            if (gateway == null)
            {
                return new GatewayPartnerRevenueReport
                {
                    StartDate = startDate,
                    EndDate = endDate
                };
            }

            // Get total order count for this gateway for monitoring
            var totalGatewayOrderCount = await GetOrderCountForGatewayAsync(gateway.GatewayCode);
            _logger.LogInformation("Gateway {GatewayCode}: Total orders in database: {TotalOrderCount}", 
                gateway.GatewayCode, totalGatewayOrderCount);

            // Get all orders in the date range for this specific gateway using the helper method
            var allOrders = await GetOrdersInDateRangeAsync(startDate, endDate, gateway.GatewayCode);

            _logger.LogInformation("Gateway {GatewayCode}: Found {OrderCount} orders in date range", gateway.GatewayCode, allOrders.Count);

            // Get all gateway partner assignments
            var assignments = await _context.GatewayPartnerAssignments
                .Include(ga => ga.GatewayPartner)
                .Include(ga => ga.PaymentGateway)
                .Where(ga => ga.IsActive)
                .ToListAsync();

            var report = new GatewayPartnerRevenueReport
            {
                StartDate = startDate,
                EndDate = endDate
            };

            // Calculate revenue for each order
            foreach (var order in allOrders)
            {
                var orderRevenue = await CalculateOrderRevenue(order, assignments, gatewayDetails);
                if (orderRevenue != null)
                {
                    report.OrderDetails.Add(orderRevenue);
                    report.TotalRevenue += orderRevenue.OrderTotal;
                }
                else
                {
                    // Try to create a basic order revenue without partner assignments
                    var basicOrderRevenue = await CreateBasicOrderRevenue(order, gatewayDetails);
                    if (basicOrderRevenue != null)
                    {
                        report.OrderDetails.Add(basicOrderRevenue);
                        report.TotalRevenue += basicOrderRevenue.OrderTotal;
                        _logger.LogInformation("Gateway {GatewayCode}: Order {OrderId} included with basic revenue calculation", 
                            gateway.GatewayCode, order.Id);
                    }
                }
            }

            // Generate gateway summaries for this specific gateway
            report.GatewaySummaries = await GenerateGatewaySummaries(report.OrderDetails, gatewayDetails);

            // Generate partner summaries
            report.PartnerSummaries = await GeneratePartnerSummaries(report.OrderDetails, assignments);

            _logger.LogInformation("Gateway {GatewayCode}: Revenue calculation completed. Total revenue: {TotalRevenue}, Orders: {OrderCount}", 
                gateway.GatewayCode, report.TotalRevenue, report.OrderDetails.Count);

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating revenue for gateway {GatewayId}", gatewayId);
            throw;
        }
    }

    public async Task<GatewayPartnerRevenueReport> CalculateRevenueForPartnerAsync(Guid partnerId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var report = await CalculateRevenueAsync(startDate, endDate);
            
            // Filter to only include orders where the partner has assignments
            var partnerOrders = report.OrderDetails
                .Where(o => o.PartnerRevenues.Any(p => p.PartnerId == partnerId))
                .ToList();

            return new GatewayPartnerRevenueReport
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalRevenue = partnerOrders.Sum(o => o.OrderTotal),
                OrderDetails = partnerOrders,
                GatewaySummaries = report.GatewaySummaries,
                PartnerSummaries = report.PartnerSummaries.Where(p => p.PartnerId == partnerId).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating revenue for partner {PartnerId}", partnerId);
            throw;
        }
    }

    private async Task<OrderRevenueDetail?> CalculateOrderRevenue(OrderV2 order, List<GatewayPartnerAssignment> assignments, List<PaymentGatewayDetails> gatewayDetails)
    {
        try
        {
            if (string.IsNullOrEmpty(order.PaymentGatewayCode))
            {
                _logger.LogWarning("Order {OrderId} skipped: No payment gateway code", order.Id);
                return null;
            }

            var gateway = gatewayDetails.FirstOrDefault(g => g.GatewayCode == order.PaymentGatewayCode);
            if (gateway == null)
            {
                _logger.LogWarning("Order {OrderId} skipped: Gateway {GatewayCode} not found in gateway details", 
                    order.Id, order.PaymentGatewayCode);
                return null;
            }

            // Parse order total
            if (!decimal.TryParse(order.OrderTotal, out var orderTotal))
            {
                _logger.LogWarning("Order {OrderId} skipped: Could not parse order total '{OrderTotal}'", 
                    order.Id, order.OrderTotal);
                return null;
            }

            // Calculate gateway fees
            var gatewayFees = CalculateGatewayFees(orderTotal, gateway);
            var netRevenue = orderTotal - gatewayFees;

            var orderRevenue = new OrderRevenueDetail
            {
                OrderId = order.Id,
                WcOrderId = order.WcOrderId,
                SiteName = order.Site?.Name ?? "Unknown Site",
                CustomerName = order.CustomerName,
                OrderTotal = orderTotal,
                Currency = order.Currency,
                PlacedAt = DateTime.TryParse(order.PlacedAt, out var placedDate) ? placedDate : DateTime.MinValue,
                PaymentGatewayCode = order.PaymentGatewayCode,
                GatewayFees = gatewayFees,
                NetRevenue = netRevenue
            };

            // Calculate partner revenue shares (if any exist)
            var gatewayAssignments = assignments.Where(a => a.PaymentGatewayId == gateway.Id).ToList();
            
            if (gatewayAssignments.Any())
            {
                foreach (var assignment in gatewayAssignments)
                {
                    var partnerRevenue = new OrderPartnerRevenue
                    {
                        PartnerId = assignment.GatewayPartnerId,
                        PartnerName = assignment.GatewayPartner?.PartnerName ?? "Unknown Partner",
                        PartnerCode = assignment.GatewayPartner?.PartnerCode ?? "N/A",
                        AssignmentPercentage = assignment.AssignmentPercentage,
                        RevenueShare = netRevenue * (assignment.AssignmentPercentage / 100m)
                    };

                    orderRevenue.PartnerRevenues.Add(partnerRevenue);
                }
            }
            // Note: Orders without partner assignments are still included in the report
            // They will show up in gateway summaries but won't have partner revenue

            return orderRevenue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating revenue for order {OrderId}", order.Id);
            return null;
        }
    }

    private async Task<OrderRevenueDetail?> CreateBasicOrderRevenue(OrderV2 order, List<PaymentGatewayDetails> gatewayDetails)
    {
        try
        {
            if (string.IsNullOrEmpty(order.PaymentGatewayCode))
            {
                return null;
            }

            var gateway = gatewayDetails.FirstOrDefault(g => g.GatewayCode == order.PaymentGatewayCode);
            if (gateway == null)
            {
                return null;
            }

            // Parse order total
            if (!decimal.TryParse(order.OrderTotal, out var orderTotal))
            {
                return null;
            }

            // Calculate gateway fees
            var gatewayFees = CalculateGatewayFees(orderTotal, gateway);
            var netRevenue = orderTotal - gatewayFees;

            var orderRevenue = new OrderRevenueDetail
            {
                OrderId = order.Id,
                WcOrderId = order.WcOrderId,
                SiteName = order.Site?.Name ?? "Unknown Site",
                CustomerName = order.CustomerName,
                OrderTotal = orderTotal,
                Currency = order.Currency,
                PlacedAt = DateTime.TryParse(order.PlacedAt, out var placedDate) ? placedDate : DateTime.MinValue,
                PaymentGatewayCode = order.PaymentGatewayCode,
                GatewayFees = gatewayFees,
                NetRevenue = netRevenue
            };

            // No partner revenues for this order - it will still appear in gateway summaries
            // but won't contribute to partner revenue calculations

            return orderRevenue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating basic revenue for order {OrderId}", order.Id);
            return null;
        }
    }

    private decimal CalculateGatewayFees(decimal orderTotal, PaymentGatewayDetails gateway)
    {
        if (gateway.FeeType?.ToLower() == "percentage" && gateway.FeesPercentage.HasValue)
        {
            return orderTotal * (gateway.FeesPercentage.Value / 100m);
        }
        else if (gateway.FeeType?.ToLower() == "fixed" && gateway.FeesFixed.HasValue)
        {
            return gateway.FeesFixed.Value;
        }
        
        return 0m;
    }

    private async Task<List<GatewayRevenueSummary>> GenerateGatewaySummaries(List<OrderRevenueDetail> orders, List<PaymentGatewayDetails> gatewayDetails)
    {
        var summaries = new List<GatewayRevenueSummary>();

        var gatewayGroups = orders.GroupBy(o => o.PaymentGatewayCode);

        foreach (var group in gatewayGroups)
        {
            var gateway = gatewayDetails.FirstOrDefault(g => g.GatewayCode == group.Key);
            if (gateway == null) continue;

            var summary = new GatewayRevenueSummary
            {
                GatewayId = gateway.Id,
                GatewayCode = gateway.GatewayCode,
                GatewayName = gateway.Descriptor ?? gateway.GatewayCode,
                TotalRevenue = group.Sum(o => o.OrderTotal),
                TotalFees = group.Sum(o => o.GatewayFees),
                NetRevenue = group.Sum(o => o.NetRevenue),
                OrderCount = group.Count()
            };

            summaries.Add(summary);
        }

        return summaries;
    }

    private async Task<List<PartnerRevenueSummary>> GeneratePartnerSummaries(List<OrderRevenueDetail> orders, List<GatewayPartnerAssignment> assignments)
    {
        var summaries = new List<PartnerRevenueSummary>();

        var partnerGroups = orders
            .SelectMany(o => o.PartnerRevenues)
            .GroupBy(p => p.PartnerId);

        foreach (var group in partnerGroups)
        {
            var firstPartner = group.First();
            var partner = assignments.FirstOrDefault(a => a.GatewayPartnerId == firstPartner.PartnerId)?.GatewayPartner;
            if (partner == null) continue;

            var summary = new PartnerRevenueSummary
            {
                PartnerId = partner.Id,
                PartnerName = partner.PartnerName,
                PartnerCode = partner.PartnerCode,
                TotalRevenueShare = group.Sum(p => p.RevenueShare),
                OrderCount = group.Count()
            };

            // Group by gateway for detailed breakdown
            var gatewayGroups = group.GroupBy(p => p.AssignmentPercentage);
            foreach (var gatewayGroup in gatewayGroups)
            {
                var gatewayRevenue = new PartnerGatewayRevenue
                {
                    GatewayCode = "N/A", // We don't have gateway code in OrderPartnerRevenue
                    RevenueShare = gatewayGroup.Sum(p => p.RevenueShare),
                    AssignmentPercentage = gatewayGroup.Average(p => p.AssignmentPercentage),
                    OrderCount = gatewayGroup.Count()
                };

                summary.GatewayRevenues.Add(gatewayRevenue);
            }

            summaries.Add(summary);
        }

        return summaries;
    }

    private async Task<List<OrderV2>> GetOrdersInDateRangeAsync(DateTime startDate, DateTime endDate, string? gatewayCode = null)
    {
        var allOrders = new List<OrderV2>();
        var pageSize = 1000; // Process orders in batches
        var page = 0;
        var hasMoreOrders = true;
        var totalProcessed = 0;
        var totalFiltered = 0;

        _logger.LogInformation("Starting to fetch orders for date range {StartDate} to {EndDate}, Gateway: {GatewayCode}", 
            startDate, endDate, gatewayCode ?? "ALL");

        while (hasMoreOrders)
        {
            var query = _context.OrdersV2
                .Include(o => o.Site)
                .Where(o => !string.IsNullOrEmpty(o.PlacedAt) &&
                           o.Status.ToLower() != "cancelled" && 
                           o.Status.ToLower() != "refunded");

            // Add gateway filter if specified
            if (!string.IsNullOrEmpty(gatewayCode))
            {
                query = query.Where(o => o.PaymentGatewayCode == gatewayCode);
            }

            var orders = await query
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            if (orders.Count == 0)
            {
                hasMoreOrders = false;
                break;
            }

            totalProcessed += orders.Count;

            // Filter orders by date range for this batch
            var filteredOrders = new List<OrderV2>();
            var dateParseErrors = 0;
            var dateOutOfRange = 0;

            foreach (var order in orders)
            {
                if (DateTime.TryParse(order.PlacedAt, out var placedDate))
                {
                    if (placedDate >= startDate && placedDate <= endDate)
                    {
                        filteredOrders.Add(order);
                    }
                    else
                    {
                        dateOutOfRange++;
                    }
                }
                else
                {
                    dateParseErrors++;
                    _logger.LogWarning("Could not parse date for order {OrderId}: {PlacedAt}", order.Id, order.PlacedAt);
                }
            }

            allOrders.AddRange(filteredOrders);
            totalFiltered += filteredOrders.Count;
            page++;

            _logger.LogInformation("Page {Page}: Processed {TotalCount} orders, {FilteredCount} in date range, {DateOutOfRange} out of range, {DateParseErrors} parse errors", 
                page, orders.Count, filteredOrders.Count, dateOutOfRange, dateParseErrors);

            // Safety check to prevent infinite loops
            if (page > 100)
            {
                _logger.LogWarning("Reached maximum page limit, stopping order processing");
                break;
            }
        }

        _logger.LogInformation("Order fetching completed: Total processed: {TotalProcessed}, Total filtered: {TotalFiltered}, Pages: {Pages}", 
            totalProcessed, totalFiltered, page);

        return allOrders;
    }

    private async Task<int> GetTotalOrderCountAsync()
    {
        return await _context.OrdersV2
            .Where(o => o.Status.ToLower() != "cancelled" && o.Status.ToLower() != "refunded")
            .CountAsync();
    }

    private async Task<int> GetOrderCountForGatewayAsync(string gatewayCode)
    {
        return await _context.OrdersV2
            .Where(o => o.PaymentGatewayCode == gatewayCode &&
                       o.Status.ToLower() != "cancelled" && 
                       o.Status.ToLower() != "refunded")
            .CountAsync();
    }
}
