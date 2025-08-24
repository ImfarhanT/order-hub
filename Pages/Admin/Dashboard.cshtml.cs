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
        TotalOrders = await _context.Orders.CountAsync();
        TotalRevenue = await _context.Orders.SumAsync(o => o.OrderTotal);
        ActivePartners = await _context.Partners.CountAsync();

        // Get recent orders
        RecentOrders = await _context.Orders
            .Include(o => o.Site)
            .OrderByDescending(o => o.PlacedAt)
            .Take(10)
            .Select(o => new OrderSummary
            {
                Id = o.Id,
                WcOrderId = o.WcOrderId,
                SiteName = o.Site.Name,
                CustomerName = o.CustomerName,
                OrderTotal = o.OrderTotal,
                Status = o.Status,
                PlacedAt = o.PlacedAt
            })
            .ToListAsync();

        // Get site statistics
        SiteStats = await _context.Sites
            .Where(s => s.IsActive)
            .Select(s => new SiteStat
            {
                SiteName = s.Name,
                OrderCount = _context.Orders.Count(o => o.SiteId == s.Id),
                TotalRevenue = _context.Orders.Where(o => o.SiteId == s.Id).Sum(o => o.OrderTotal)
            })
            .ToListAsync();
    }
}
