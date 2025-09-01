using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HubApi.Data;
using HubApi.Models;
using HubApi.DTOs;

namespace HubApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ProfitController : ControllerBase
{
    private readonly OrderHubDbContext _context;
    private readonly ILogger<ProfitController> _logger;

    public ProfitController(OrderHubDbContext context, ILogger<ProfitController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all profit calculations
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetOrderProfits(
        [FromQuery] Guid? siteId = null,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] string? payoutStatus = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            _logger.LogInformation("GetOrderProfits called with: siteId={SiteId}, search={Search}, status={Status}, payoutStatus={PayoutStatus}, page={Page}, pageSize={PageSize}", 
                siteId, search, status, payoutStatus, page, pageSize);

            // Validate page size
            if (pageSize <= 0 || pageSize > 100)
            {
                pageSize = 50;
            }

            // Build the base query
            var ordersQuery = _context.OrdersV2.AsQueryable();

            // Always exclude cancelled and refunded orders from profit calculations
            ordersQuery = ordersQuery.Where(o => o.Status.ToLower() != "cancelled" && o.Status.ToLower() != "refunded");

            // Apply site filter if provided
            if (siteId.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.SiteId == siteId.Value);
            }

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(search))
            {
                ordersQuery = ordersQuery.Where(o => 
                    o.WcOrderId.Contains(search) ||
                    o.CustomerName.Contains(search));
            }

            if (!string.IsNullOrEmpty(status))
            {
                // Additional status filter (but still excluding cancelled/refunded)
                ordersQuery = ordersQuery.Where(o => o.Status.ToLower() == status.ToLower());
            }

            // Get total count for pagination
            var totalCount = await ordersQuery.CountAsync();
            _logger.LogInformation("Total orders found: {TotalCount}", totalCount);

            // Apply pagination
            var orders = await ordersQuery
                .OrderByDescending(o => o.SyncedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new
                {
                    o.Id,
                    o.WcOrderId,
                    o.SiteId,
                    o.CustomerName,
                    o.OrderTotal,
                    o.Status,
                    o.SyncedAt
                })
                .ToListAsync();

            _logger.LogInformation("Retrieved {OrderCount} orders for page {Page}", orders.Count, page);

            // Get site names
            var siteIds = orders.Select(o => o.SiteId).Distinct().ToList();
            var sites = await _context.Sites
                .Where(s => siteIds.Contains(s.Id))
                .Select(s => new { s.Id, s.Name })
                .ToDictionaryAsync(s => s.Id, s => s.Name);

            // Get profit data for these orders
            var orderIds = orders.Select(o => o.Id).ToList();
            var profits = await _context.OrderProfits
                .Where(op => orderIds.Contains(op.OrderId))
                .ToDictionaryAsync(op => op.OrderId, op => op);

            _logger.LogInformation("Retrieved profit data for {ProfitCount} orders", profits.Count);

            // Combine orders with profit data
            var result = orders.Select(order => {
                var profit = profits.GetValueOrDefault(order.Id);
                var siteName = sites.GetValueOrDefault(order.SiteId, "Unknown Site");
                
                return new
                {
                    OrderId = order.Id,
                    order.WcOrderId,
                    order.SiteId,
                    SiteName = siteName,
                    order.CustomerName,
                    order.OrderTotal,
                    order.Status,
                    order.SyncedAt,
                    // Profit calculation data (if exists)
                    ProfitId = profit?.Id,
                    ProductCost = profit?.ProductCost ?? 0,
                    GatewayCost = profit?.GatewayCost ?? 0,
                    OperationalCost = profit?.OperationalCost ?? 0,
                    TotalCosts = profit?.TotalCosts ?? 0,
                    NetProfit = profit?.NetProfit ?? 0,
                    ProfitMargin = profit?.ProfitMargin ?? 0,
                    PayoutStatus = profit?.PayoutStatus ?? "processing",
                    PayoutDate = profit?.PayoutDate,
                    IsCalculated = profit?.IsCalculated ?? false,
                    Notes = profit?.Notes ?? "",
                    ProfitUpdatedAt = profit?.UpdatedAt
                };
            }).ToList();

            // Apply payout status filter after combining data
            if (!string.IsNullOrEmpty(payoutStatus))
            {
                result = result.Where(o => o.PayoutStatus == payoutStatus).ToList();
                _logger.LogInformation("After payout status filter: {FilteredCount} orders", result.Count);
            }

            Response.Headers.Add("X-Total-Count", totalCount.ToString());
            Response.Headers.Add("X-Page", page.ToString());
            Response.Headers.Add("X-Page-Size", pageSize.ToString());

            _logger.LogInformation("Successfully returning {ResultCount} orders", result.Count);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders with profit calculations: {Message}", ex.Message);
            _logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Get profit calculation for a specific order
    /// </summary>
    [HttpGet("{orderId}")]
    public async Task<ActionResult<object>> GetOrderProfit(Guid orderId)
    {
        try
        {
            var profit = await _context.OrderProfits
                .Include(op => op.Order)
                .Include(op => op.Site)
                .Where(op => op.OrderId == orderId)
                .Select(op => new
                {
                    op.Id,
                    op.OrderId,
                    op.WcOrderId,
                    op.SiteId,
                    SiteName = op.Site.Name,
                    op.OrderTotal,
                    op.ProductCost,
                    op.GatewayCost,
                    op.OperationalCost,
                    op.TotalCosts,
                    op.NetProfit,
                    op.ProfitMargin,
                    op.PayoutStatus,
                    op.PayoutDate,
                    op.IsCalculated,
                    op.Notes,
                    op.CreatedAt,
                    op.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (profit == null)
            {
                return NotFound(new { error = "Profit calculation not found for this order" });
            }

            return Ok(profit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving profit for order {OrderId}", orderId);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Create or update profit calculation for an order
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<object>> CalculateOrderProfit([FromBody] OrderProfitRequest request)
    {
        try
        {
            // Get the order details
            var order = await _context.OrdersV2
                .Include(o => o.Site)
                .FirstOrDefaultAsync(o => o.Id == request.OrderId);

            if (order == null)
            {
                return NotFound(new { error = "Order not found" });
            }

            // Parse order total
            if (!decimal.TryParse(order.OrderTotal, out var orderTotal))
            {
                return BadRequest(new { error = "Invalid order total format" });
            }

            // Calculate costs based on payout status
            var gatewayCost = orderTotal * (request.GatewayCostPercentage / 100m);
            var operationalCost = 5.00m; // Fixed $5.00 per order
            var totalCosts = request.ProductCost + gatewayCost + operationalCost;
            
            decimal netProfit;
            decimal profitMargin;
            
            // Profit calculation varies based on payout status
            switch (request.PayoutStatus?.ToLower())
            {
                case "paid":
                    // Full profit calculation - normal case
                    netProfit = orderTotal - totalCosts;
                    profitMargin = orderTotal > 0 ? (netProfit / orderTotal) * 100 : 0;
                    break;
                    
                case "processing":
                    // Subtract gateway fees and product cost from current profit
                    // This represents potential loss if order doesn't complete
                    netProfit = -(gatewayCost + request.ProductCost);
                    profitMargin = 0; // No margin for processing orders
                    break;
                    
                case "refunded":
                    // Complete loss - order total + gateway fees + product cost
                    netProfit = -(orderTotal + gatewayCost + request.ProductCost);
                    profitMargin = -100; // 100% loss
                    break;
                    
                default:
                    // Default to processing status
                    netProfit = -(gatewayCost + request.ProductCost);
                    profitMargin = 0;
                    break;
            }

            // Check if profit calculation already exists
            var existingProfit = await _context.OrderProfits
                .FirstOrDefaultAsync(op => op.OrderId == request.OrderId);

            if (existingProfit != null)
            {
                // Update existing profit calculation
                existingProfit.ProductCost = request.ProductCost;
                existingProfit.GatewayCostPercentage = request.GatewayCostPercentage;
                existingProfit.GatewayCost = gatewayCost;
                existingProfit.OperationalCost = operationalCost;
                existingProfit.TotalCosts = totalCosts;
                existingProfit.NetProfit = netProfit;
                existingProfit.ProfitMargin = profitMargin;
                existingProfit.PayoutStatus = request.PayoutStatus ?? "processing";
                existingProfit.PayoutDate = request.PayoutStatus == "paid" ? DateTime.UtcNow : existingProfit.PayoutDate;
                existingProfit.Notes = request.Notes;
                existingProfit.IsCalculated = true;
                existingProfit.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation("Updated profit calculation for order {OrderId}: Status: {PayoutStatus}, Net Profit: {NetProfit}, Margin: {ProfitMargin}%", 
                    request.OrderId, request.PayoutStatus, netProfit, profitMargin);
            }
            else
            {
                // Create new profit calculation
                var profit = new OrderProfit
                {
                    Id = Guid.NewGuid(),
                    OrderId = request.OrderId,
                    SiteId = order.SiteId,
                    WcOrderId = order.WcOrderId,
                    OrderTotal = orderTotal,
                    ProductCost = request.ProductCost,
                    GatewayCostPercentage = request.GatewayCostPercentage,
                    GatewayCost = gatewayCost,
                    OperationalCost = operationalCost,
                    TotalCosts = totalCosts,
                    NetProfit = netProfit,
                    ProfitMargin = profitMargin,
                    PayoutStatus = request.PayoutStatus ?? "processing",
                    PayoutDate = request.PayoutStatus == "paid" ? DateTime.UtcNow : null,
                    Notes = request.Notes,
                    IsCalculated = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.OrderProfits.Add(profit);

                _logger.LogInformation("Created profit calculation for order {OrderId}: Status: {PayoutStatus}, Net Profit: {NetProfit}, Margin: {ProfitMargin}%", 
                    request.OrderId, request.PayoutStatus, netProfit, profitMargin);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Profit calculation saved successfully",
                data = new
                {
                    orderId = request.OrderId,
                    wcOrderId = order.WcOrderId,
                    orderTotal = orderTotal,
                    productCost = request.ProductCost,
                    gatewayCostPercentage = request.GatewayCostPercentage,
                    gatewayCost = gatewayCost,
                    operationalCost = operationalCost,
                    totalCosts = totalCosts,
                    netProfit = netProfit,
                    profitMargin = profitMargin,
                    payoutStatus = request.PayoutStatus ?? "processing"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating profit for order {OrderId}", request.OrderId);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Update payout status for an order
    /// </summary>
    [HttpPut("status/{orderId}")]
    public async Task<ActionResult<object>> UpdatePayoutStatus(Guid orderId, [FromBody] UpdatePayoutStatusRequest request)
    {
        try
        {
            // Check if profit calculation exists
            var existingProfit = await _context.OrderProfits
                .FirstOrDefaultAsync(op => op.OrderId == orderId);

            if (existingProfit == null)
            {
                // Create a basic profit record if none exists
                var order = await _context.OrdersV2
                    .Include(o => o.Site)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    return NotFound(new { error = "Order not found" });
                }

                // Parse order total
                if (!decimal.TryParse(order.OrderTotal, out var orderTotal))
                {
                    return BadRequest(new { error = "Invalid order total format" });
                }

                // Create basic profit record with default values
                existingProfit = new OrderProfit
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    SiteId = order.SiteId,
                    WcOrderId = order.WcOrderId,
                    OrderTotal = orderTotal,
                    ProductCost = 0,
                    GatewayCostPercentage = 0,
                    GatewayCost = 0,
                    OperationalCost = 5.00m,
                    TotalCosts = 5.00m,
                    NetProfit = orderTotal - 5.00m,
                    ProfitMargin = orderTotal > 0 ? ((orderTotal - 5.00m) / orderTotal) * 100 : 0,
                    PayoutStatus = request.PayoutStatus,
                    PayoutDate = request.PayoutStatus == "paid" ? DateTime.UtcNow : null,
                    IsCalculated = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.OrderProfits.Add(existingProfit);
            }
            else
            {
                // Update existing profit record
                existingProfit.PayoutStatus = request.PayoutStatus;
                existingProfit.PayoutDate = request.PayoutStatus == "paid" ? DateTime.UtcNow : existingProfit.PayoutDate;
                existingProfit.UpdatedAt = DateTime.UtcNow;

                // Recalculate profit based on new payout status
                if (existingProfit.IsCalculated)
                {
                    var orderTotal = existingProfit.OrderTotal;
                    var productCost = existingProfit.ProductCost;
                    var gatewayCost = existingProfit.GatewayCost;
                    var operationalCost = existingProfit.OperationalCost;

                    decimal netProfit;
                    decimal profitMargin;

                    switch (request.PayoutStatus?.ToLower())
                    {
                        case "paid":
                            netProfit = orderTotal - (productCost + gatewayCost + operationalCost);
                            profitMargin = orderTotal > 0 ? (netProfit / orderTotal) * 100 : 0;
                            break;
                        case "processing":
                            netProfit = -(gatewayCost + productCost);
                            profitMargin = 0;
                            break;
                        case "refunded":
                            netProfit = -(orderTotal + gatewayCost + productCost);
                            profitMargin = -100;
                            break;
                        default:
                            netProfit = -(gatewayCost + productCost);
                            profitMargin = 0;
                            break;
                    }

                    existingProfit.NetProfit = netProfit;
                    existingProfit.ProfitMargin = profitMargin;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated payout status for order {OrderId} to {PayoutStatus}", orderId, request.PayoutStatus);

            return Ok(new
            {
                success = true,
                message = "Payout status updated successfully",
                data = new
                {
                    orderId = orderId,
                    payoutStatus = request.PayoutStatus,
                    payoutDate = existingProfit.PayoutDate
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payout status for order {OrderId}", orderId);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Get profit summary statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<object>> GetProfitStats(
        [FromQuery] Guid? siteId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var query = _context.OrderProfits.AsQueryable();

            if (siteId.HasValue)
                query = query.Where(op => op.SiteId == siteId.Value);

            // Note: Date filtering disabled for now since we need to parse dates
            // TODO: Implement proper date filtering

            var stats = await query
                .GroupBy(op => op.SiteId)
                .Select(g => new
                {
                    site_id = g.Key,
                    total_orders = g.Count(),
                    total_revenue = g.Sum(op => op.OrderTotal),
                    total_costs = g.Sum(op => op.TotalCosts),
                    total_profit = g.Sum(op => op.NetProfit),
                    average_profit_margin = g.Average(op => op.ProfitMargin)
                })
                .ToListAsync();

            var totalStats = new
            {
                total_orders = await query.CountAsync(),
                total_revenue = await query.SumAsync(op => op.OrderTotal),
                total_costs = await query.SumAsync(op => op.TotalCosts),
                total_profit = await query.SumAsync(op => op.NetProfit),
                average_profit_margin = await query.AverageAsync(op => op.ProfitMargin),
                by_site = stats
            };

            return Ok(totalStats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving profit statistics");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Test endpoint to check database connectivity
    /// </summary>
    [HttpGet("test")]
    public async Task<ActionResult<object>> TestDatabase()
    {
        try
        {
            var orderCount = await _context.OrdersV2.CountAsync();
            var siteCount = await _context.Sites.CountAsync();
            var profitCount = await _context.OrderProfits.CountAsync();
            
            return Ok(new
            {
                success = true,
                message = "Database connection successful",
                data = new
                {
                    ordersCount = orderCount,
                    sitesCount = siteCount,
                    profitsCount = profitCount,
                    timestamp = DateTime.UtcNow
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database test failed: {Message}", ex.Message);
            return StatusCode(500, new { error = "Database test failed", details = ex.Message });
        }
    }
}
