using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HubApi.Data;
using HubApi.Models;
using HubApi.DTOs;

namespace HubApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class OrdersV2Controller : ControllerBase
{
    private readonly OrderHubDbContext _context;
    private readonly ILogger<OrdersV2Controller> _logger;

    public OrdersV2Controller(OrderHubDbContext context, ILogger<OrdersV2Controller> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        try
        {
            var order = await _context.OrdersV2
                .Include(o => o.Site)
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound(new { error = "Order not found" });
            }

            var result = new
            {
                id = order.Id,
                wcOrderId = order.WcOrderId,
                siteName = order.Site?.Name ?? "Unknown Site",
                status = order.Status,
                currency = order.Currency,
                orderTotal = order.OrderTotal,
                subtotal = order.Subtotal,
                discountTotal = order.DiscountTotal,
                shippingTotal = order.ShippingTotal,
                taxTotal = order.TaxTotal,
                paymentGatewayCode = order.PaymentGatewayCode,
                customerName = order.CustomerName,
                customerEmail = order.CustomerEmail,
                customerPhone = order.CustomerPhone,
                shippingAddress = order.ShippingAddress,
                billingAddress = order.BillingAddress,
                placedAt = order.PlacedAt,
                syncedAt = order.SyncedAt,
                orderItems = order.OrderItems.Select(item => new
                {
                    id = item.Id,
                    productId = item.ProductId,
                    sku = item.Sku,
                    name = item.Name,
                    qty = item.Qty,
                    price = item.Price,
                    subtotal = item.Subtotal,
                    total = item.Total
                }).ToList()
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order {OrderId}", id);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var totalCount = await _context.OrdersV2.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var orders = await _context.OrdersV2
                .Include(o => o.Site)
                .OrderByDescending(o => o.SyncedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new
                {
                    id = o.Id,
                    wcOrderId = o.WcOrderId,
                    siteName = o.Site != null ? o.Site.Name : "Unknown Site",
                    customerName = o.CustomerName,
                    customerEmail = o.CustomerEmail,
                    orderTotal = o.OrderTotal,
                    subtotal = o.Subtotal,
                    currency = o.Currency,
                    status = o.Status,
                    paymentGatewayCode = o.PaymentGatewayCode,
                    placedAt = o.PlacedAt,
                    syncedAt = o.SyncedAt
                })
                .ToListAsync();

            return Ok(new
            {
                orders,
                pagination = new
                {
                    page,
                    pageSize,
                    totalCount,
                    totalPages
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    // PUT: api/v1/ordersv2/{id}/gateway
    [HttpPut("{id:guid}/gateway")]
    public async Task<IActionResult> UpdateOrderGateway(Guid id, [FromBody] UpdateOrderGatewayRequest request)
    {
        try
        {
            var order = await _context.OrdersV2.FindAsync(id);
            if (order == null)
            {
                return NotFound(new { error = "Order not found" });
            }

            // Validate the payment gateway code
            if (string.IsNullOrWhiteSpace(request.PaymentGatewayCode))
            {
                return BadRequest(new { error = "Payment gateway code is required" });
            }

            // Update the payment gateway
            order.PaymentGatewayCode = request.PaymentGatewayCode;
            order.SyncedAt = DateTime.UtcNow; // Update sync time to reflect the change

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated payment gateway for order {OrderId} to {GatewayCode}", id, request.PaymentGatewayCode);

            return Ok(new { message = "Payment gateway updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment gateway for order {OrderId}", id);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }
}
