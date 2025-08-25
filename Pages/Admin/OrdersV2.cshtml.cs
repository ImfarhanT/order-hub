using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HubApi.Data;
using HubApi.Models;

namespace HubApi.Pages.Admin;

public class OrdersV2Model : PageModel
{
    private readonly OrderHubDbContext _context;
    private readonly ILogger<OrdersV2Model> _logger;

    public OrdersV2Model(OrderHubDbContext context, ILogger<OrdersV2Model> logger)
    {
        _context = context;
        _logger = logger;
    }

    public List<OrderV2ViewModel> Orders { get; set; } = new List<OrderV2ViewModel>();
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }

    public async Task<IActionResult> OnGetAsync(int? page, int? pageSize)
    {
        try
        {
            PageNumber = page ?? 1;
            PageSize = pageSize ?? 20;

            // Get total count
            TotalCount = await _context.OrdersV2.CountAsync();
            TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);

            // Get orders with pagination
            var ordersQuery = _context.OrdersV2
                .Include(o => o.Site)
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.SyncedAt);

            var orders = await ordersQuery
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Convert to view models
            Orders = orders.Select(o => new OrderV2ViewModel
            {
                Id = o.Id,
                WcOrderId = o.WcOrderId,
                SiteName = o.Site?.Name ?? "Unknown Site",
                CustomerName = o.CustomerName,
                CustomerEmail = o.CustomerEmail,
                OrderTotal = o.OrderTotal,
                Subtotal = o.Subtotal,
                Currency = o.Currency,
                Status = o.Status,
                PaymentGatewayCode = o.PaymentGatewayCode,
                PlacedAt = o.PlacedAt,
                SyncedAt = o.SyncedAt
            }).ToList();

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading orders V2");
            return Page();
        }
    }
}

public class OrderV2ViewModel
{
    public Guid Id { get; set; }
    public string WcOrderId { get; set; } = string.Empty;
    public string SiteName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string OrderTotal { get; set; } = string.Empty;
    public string Subtotal { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PaymentGatewayCode { get; set; } = string.Empty;
    public string PlacedAt { get; set; } = string.Empty;
    public DateTime SyncedAt { get; set; }
}
