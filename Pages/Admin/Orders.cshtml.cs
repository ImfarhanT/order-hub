using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HubApi.Data;
using HubApi.Models;
using HubApi.Pages.Models;

namespace HubApi.Pages.Admin;

[Authorize]
public class OrdersModel : PageModel
{
    private readonly OrderHubDbContext _context;

    public OrdersModel(OrderHubDbContext context)
    {
        _context = context;
    }

    public List<OrderSummary> Orders { get; set; } = new();

    public async Task OnGetAsync()
    {
        Orders = await _context.Orders
            .Include(o => o.Site)
            .OrderByDescending(o => o.PlacedAt)
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
    }
}
