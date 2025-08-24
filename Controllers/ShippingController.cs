using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HubApi.Data;
using HubApi.DTOs;
using HubApi.Models;

namespace HubApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ShippingController : ControllerBase
{
    private readonly OrderHubDbContext _context;
    private readonly ILogger<ShippingController> _logger;

    public ShippingController(OrderHubDbContext context, ILogger<ShippingController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost("update")]
    public async Task<IActionResult> UpdateShipping([FromBody] ShippingUpdateRequest request)
    {
        try
        {
            var site = HttpContext.Items["Site"] as Site;
            if (site == null)
                return Unauthorized(new { error = "Site not authenticated" });

            // Find the order
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.SiteId == site.Id && o.WcOrderId == request.WcOrderId);

            if (order == null)
                return NotFound(new { error = "Order not found" });

            // Create shipping update
            var shippingUpdate = new ShippingUpdate
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Status = request.Status,
                Provider = request.Provider,
                TrackingNumber = request.TrackingNumber,
                Payload = request.Payload,
                OccurredAt = request.OccurredAt
            };

            _context.ShippingUpdates.Add(shippingUpdate);

            // Update order status if it's a terminal state
            if (IsTerminalStatus(request.Status))
            {
                order.Status = request.Status;
            }

            await _context.SaveChangesAsync();

            return Ok(new { ok = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating shipping for order {OrderId}", request.WcOrderId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    private static bool IsTerminalStatus(string status)
    {
        return status.Equals("delivered", StringComparison.OrdinalIgnoreCase) ||
               status.Equals("failed", StringComparison.OrdinalIgnoreCase) ||
               status.Equals("cancelled", StringComparison.OrdinalIgnoreCase);
    }
}
