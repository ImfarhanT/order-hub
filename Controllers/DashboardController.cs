using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HubApi.Data;
using HubApi.Models;
using System.Globalization;

namespace HubApi.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly OrderHubDbContext _context;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(OrderHubDbContext context, ILogger<DashboardController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<object>> GetDashboardStats()
    {
        try
        {
            // Get basic counts
            var totalSites = await _context.Sites.CountAsync(s => s.IsActive);
            var totalOrders = await _context.OrdersV2.CountAsync();
            var activePartners = await _context.Partners.CountAsync();

            // Get all orders for revenue calculation
            var allOrders = await _context.OrdersV2.ToListAsync();
            var totalRevenue = allOrders.Sum(o => decimal.Parse(o.OrderTotal));

            return Ok(new
            {
                success = true,
                data = new
                {
                    totalSites,
                    totalOrders,
                    totalRevenue,
                    activePartners
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard stats: {Message}", ex.Message);
            return StatusCode(500, new { error = "Failed to get dashboard stats", details = ex.Message });
        }
    }

    [HttpGet("revenue-trend")]
    public async Task<ActionResult<object>> GetRevenueTrend([FromQuery] string period = "6m")
    {
        try
        {
            var endDate = DateTime.UtcNow;
            var startDate = period switch
            {
                "7d" => endDate.AddDays(-7),
                "30d" => endDate.AddDays(-30),
                "90d" => endDate.AddDays(-90),
                "6m" => endDate.AddMonths(-6),
                "1y" => endDate.AddYears(-1),
                _ => endDate.AddMonths(-6)
            };

            _logger.LogInformation("Fetching revenue trend from {startDate} to {endDate}", startDate, endDate);

            // Get orders within the date range
            var orders = await _context.OrdersV2
                .Where(o => !string.IsNullOrEmpty(o.PlacedAt))
                .ToListAsync();

            // Filter orders by date range and handle date parsing safely
            var filteredOrders = orders
                .Where(o => {
                    if (DateTime.TryParse(o.PlacedAt, out var placedDate))
                    {
                        return placedDate >= startDate && placedDate <= endDate;
                    }
                    return false;
                })
                .ToList();

            _logger.LogInformation("Found {orderCount} orders in date range", filteredOrders.Count);

            // Group by month
            var monthlyData = filteredOrders
                .GroupBy(o => {
                    var placedDate = DateTime.Parse(o.PlacedAt);
                    return new { 
                        Month = placedDate.ToString("MMM yyyy"), 
                        Year = placedDate.Year, 
                        MonthNum = placedDate.Month 
                    };
                })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.MonthNum)
                .Select(g => new
                {
                    month = g.Key.Month,
                    revenue = g.Sum(o => {
                        if (decimal.TryParse(o.OrderTotal, out var total))
                            return total;
                        return 0m;
                    }),
                    orders = g.Count()
                })
                .ToList();

            _logger.LogInformation("Generated {monthCount} months of data", monthlyData.Count);

            return Ok(new
            {
                success = true,
                data = monthlyData
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting revenue trend: {Message}", ex.Message);
            return StatusCode(500, new { error = "Failed to get revenue trend", details = ex.Message });
        }
    }

    [HttpGet("order-status-distribution")]
    public async Task<ActionResult<object>> GetOrderStatusDistribution()
    {
        try
        {
            _logger.LogInformation("Fetching order status distribution");

            var statusDistribution = await _context.OrdersV2
                .Where(o => !string.IsNullOrEmpty(o.Status))
                .GroupBy(o => o.Status)
                .Select(g => new
                {
                    status = g.Key,
                    count = g.Count()
                })
                .ToListAsync();

            _logger.LogInformation("Found {statusCount} different statuses", statusDistribution.Count);

            // Map statuses to user-friendly names
            var mappedStatuses = statusDistribution.Select(s => new
            {
                status = MapStatus(s.status),
                count = s.count
            }).ToList();

            return Ok(new
            {
                success = true,
                data = mappedStatuses
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order status distribution: {Message}", ex.Message);
            return StatusCode(500, new { error = "Failed to get order status distribution", details = ex.Message });
        }
    }

    [HttpGet("site-performance")]
    public async Task<ActionResult<object>> GetSitePerformance()
    {
        try
        {
            _logger.LogInformation("Fetching site performance data");

            // Get all sites first
            var sites = await _context.Sites
                .Where(s => s.IsActive)
                .ToListAsync();

            var siteStats = new List<object>();

            foreach (var site in sites)
            {
                var orderCount = await _context.OrdersV2.CountAsync(o => o.SiteId == site.Id);
                
                var siteOrders = await _context.OrdersV2
                    .Where(o => o.SiteId == site.Id && 
                               !string.IsNullOrEmpty(o.OrderTotal) &&
                               o.Status.ToLower() != "cancelled" && 
                               o.Status.ToLower() != "refunded")
                    .ToListAsync();
                
                var totalRevenue = siteOrders.Sum(o => {
                    if (decimal.TryParse(o.OrderTotal, out var total))
                        return total;
                    return 0m;
                });

                siteStats.Add(new
                {
                    siteName = site.Name,
                    orderCount,
                    totalRevenue
                });
            }

            _logger.LogInformation("Found {siteCount} active sites", siteStats.Count);

            return Ok(new
            {
                success = true,
                data = siteStats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting site performance: {Message}", ex.Message);
            return StatusCode(500, new { error = "Failed to get site performance", details = ex.Message });
        }
    }

    [HttpGet("test")]
    public async Task<ActionResult<object>> TestDashboard()
    {
        try
        {
            _logger.LogInformation("Testing dashboard controller");
            
            var orderCount = await _context.OrdersV2.CountAsync();
            var siteCount = await _context.Sites.CountAsync();
            var partnerCount = await _context.Partners.CountAsync();
            
            // Get a sample order to check data structure
            var sampleOrder = await _context.OrdersV2.FirstOrDefaultAsync();
            
            return Ok(new
            {
                success = true,
                message = "Dashboard controller is working",
                data = new
                {
                    orderCount,
                    siteCount,
                    partnerCount,
                    sampleOrder = sampleOrder != null ? new
                    {
                        id = sampleOrder.Id,
                        wcOrderId = sampleOrder.WcOrderId,
                        orderTotal = sampleOrder.OrderTotal,
                        status = sampleOrder.Status,
                        placedAt = sampleOrder.PlacedAt,
                        siteId = sampleOrder.SiteId
                    } : null,
                    timestamp = DateTime.UtcNow
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dashboard test failed: {Message}", ex.Message);
            return StatusCode(500, new { error = "Dashboard test failed", details = ex.Message });
        }
    }

    private string MapStatus(string status)
    {
        return status?.ToLower() switch
        {
            "completed" => "Completed",
            "processing" => "Processing",
            "shipped" => "Shipped",
            "pending" => "Pending",
            "cancelled" => "Cancelled",
            "refunded" => "Refunded",
            _ => "Other"
        };
    }
}
