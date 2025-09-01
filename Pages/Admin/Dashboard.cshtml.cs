using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HubApi.Data;
using HubApi.Models;
using HubApi.Pages.Models;

namespace HubApi.Pages.Admin;

[Authorize]
public class DashboardModel : PageModel
{
    private readonly OrderHubDbContext _context;

    public DashboardModel(OrderHubDbContext context)
    {
        _context = context;
    }

    public int TotalSites { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public int ActivePartners { get; set; }
    public List<OrderSummary> RecentOrders { get; set; } = new();
    public List<SiteStat> SiteStats { get; set; } = new();

    public async Task OnGetAsync()
    {
        // Get basic counts
        TotalSites = await _context.Sites.CountAsync(s => s.IsActive);
        TotalOrders = await _context.OrdersV2.CountAsync();
        ActivePartners = await _context.Partners.CountAsync();

        // Get all orders for revenue calculation (fetch first, then calculate in memory)
        // Exclude cancelled and refunded orders from revenue calculations
        var allOrders = await _context.OrdersV2
            .Where(o => o.Status.ToLower() != "cancelled" && o.Status.ToLower() != "refunded")
            .ToListAsync();
        TotalRevenue = allOrders.Sum(o => decimal.Parse(o.OrderTotal));

        // Get recent orders
        RecentOrders = await _context.OrdersV2
            .Include(o => o.Site)
            .OrderByDescending(o => o.SyncedAt)
            .Take(10)
            .Select(o => new OrderSummary
            {
                Id = o.Id,
                WcOrderId = o.WcOrderId,
                SiteName = o.Site.Name,
                CustomerName = o.CustomerName,
                OrderTotal = 0, // Will be set after fetching
                Status = o.Status,
                PlacedAt = DateTime.UtcNow // Will be set after fetching
            })
            .ToListAsync();

        // Update the order totals and dates after fetching
        var recentOrderIds = RecentOrders.Select(o => o.Id).ToList();
        var recentOrderData = await _context.OrdersV2
            .Where(o => recentOrderIds.Contains(o.Id))
            .ToListAsync();

        foreach (var order in RecentOrders)
        {
            var orderData = recentOrderData.FirstOrDefault(o => o.Id == order.Id);
            if (orderData != null)
            {
                order.OrderTotal = decimal.Parse(orderData.OrderTotal);
                order.PlacedAt = DateTime.Parse(orderData.PlacedAt);
            }
        }

        // Get site statistics
        SiteStats = await _context.Sites
            .Where(s => s.IsActive)
            .Select(s => new SiteStat
            {
                SiteName = s.Name,
                OrderCount = _context.OrdersV2.Count(o => o.SiteId == s.Id),
                TotalRevenue = 0 // Will be calculated in memory
            })
            .ToListAsync();

        // Calculate site revenues in memory (excluding cancelled and refunded orders)
        foreach (var stat in SiteStats)
        {
            var siteOrders = allOrders.Where(o => o.SiteId == _context.Sites.First(s => s.Name == stat.SiteName).Id);
            stat.TotalRevenue = siteOrders.Sum(o => decimal.Parse(o.OrderTotal));
        }
    }
}
