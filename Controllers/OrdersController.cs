using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HubApi.Data;
using HubApi.DTOs;
using HubApi.Models;
using HubApi.Services;

namespace HubApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderHubDbContext _context;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(OrderHubDbContext context, ILogger<OrdersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost("sync")]
    public async Task<IActionResult> SyncOrder([FromBody] OrderSyncRequest request)
    {
        try
        {
            var site = HttpContext.Items["Site"] as Site;
            if (site == null)
                return Unauthorized(new { error = "Site not authenticated" });

            // Check if order already exists
            var existingOrder = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.SiteId == site.Id && o.WcOrderId == request.Order.WcOrderId);

            if (existingOrder != null)
            {
                // Update existing order
                UpdateExistingOrder(existingOrder, request);
            }
            else
            {
                // Create new order
                CreateNewOrder(site.Id, request);
            }

            await _context.SaveChangesAsync();

            // Compute revenue shares
            await ComputeRevenueShares(site.Id, request.Order.WcOrderId);

            return Ok(new { ok = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing order {OrderId}", request.Order.WcOrderId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    private void UpdateExistingOrder(Order existingOrder, OrderSyncRequest request)
    {
        // Update order properties
        existingOrder.Status = request.Order.Status;
        existingOrder.OrderTotal = request.Order.OrderTotal;
        existingOrder.Subtotal = request.Order.Subtotal;
        existingOrder.DiscountTotal = request.Order.DiscountTotal;
        existingOrder.ShippingTotal = request.Order.ShippingTotal;
        existingOrder.TaxTotal = request.Order.TaxTotal;
        existingOrder.PaymentGatewayCode = request.Order.PaymentGatewayCode;
        existingOrder.CustomerName = request.Order.CustomerName;
        existingOrder.CustomerEmail = request.Order.CustomerEmail;
        existingOrder.CustomerPhone = request.Order.CustomerPhone;
        existingOrder.ShippingAddress = request.Order.ShippingAddress;
        existingOrder.BillingAddress = request.Order.BillingAddress;
        existingOrder.SyncedAt = DateTime.UtcNow;

        // Remove existing items and add new ones
        _context.OrderItems.RemoveRange(existingOrder.OrderItems);
        existingOrder.OrderItems.Clear();

        foreach (var itemData in request.Items)
        {
            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = existingOrder.Id,
                ProductId = itemData.ProductId,
                Sku = itemData.Sku,
                Name = itemData.Name,
                Qty = itemData.Qty,
                Price = itemData.Price,
                Subtotal = itemData.Subtotal,
                Total = itemData.Total
            };
            existingOrder.OrderItems.Add(orderItem);
        }
    }

    private void CreateNewOrder(Guid siteId, OrderSyncRequest request)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            SiteId = siteId,
            WcOrderId = request.Order.WcOrderId,
            Status = request.Order.Status,
            Currency = request.Order.Currency,
            OrderTotal = request.Order.OrderTotal,
            Subtotal = request.Order.Subtotal,
            DiscountTotal = request.Order.DiscountTotal,
            ShippingTotal = request.Order.ShippingTotal,
            TaxTotal = request.Order.TaxTotal,
            PaymentGatewayCode = request.Order.PaymentGatewayCode,
            CustomerName = request.Order.CustomerName,
            CustomerEmail = request.Order.CustomerEmail,
            CustomerPhone = request.Order.CustomerPhone,
            ShippingAddress = request.Order.ShippingAddress,
            BillingAddress = request.Order.BillingAddress,
            PlacedAt = request.Order.PlacedAt,
            SyncedAt = DateTime.UtcNow
        };

        _context.Orders.Add(order);

        foreach (var itemData in request.Items)
        {
            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = itemData.ProductId,
                Sku = itemData.Sku,
                Name = itemData.Name,
                Qty = itemData.Qty,
                Price = itemData.Price,
                Subtotal = itemData.Subtotal,
                Total = itemData.Total
            };
            _context.OrderItems.Add(orderItem);
        }
    }

    private async Task ComputeRevenueShares(Guid siteId, string wcOrderId)
    {
        var order = await _context.Orders
            .Include(o => o.Site)
            .FirstOrDefaultAsync(o => o.SiteId == siteId && o.WcOrderId == wcOrderId);

        if (order == null) return;

        // Get site partners
        var sitePartners = await _context.SitePartners
            .Include(sp => sp.Partner)
            .Where(sp => sp.SiteId == siteId)
            .ToListAsync();

        // Get site gateway configuration
        var siteGateway = await _context.SiteGateways
            .Include(sg => sg.Gateway)
            .FirstOrDefaultAsync(sg => sg.SiteId == siteId && sg.Gateway.Code == order.PaymentGatewayCode);

        // Calculate website share
        var websiteSharePercent = siteGateway?.WebsiteSharePercent ?? 0;
        var websiteShareAmount = order.OrderTotal * (websiteSharePercent / 100);

        // Calculate partner shares
        foreach (var sitePartner in sitePartners)
        {
            var partnerShareAmount = order.OrderTotal * (sitePartner.SharePercent / 100);
            var gatewayFeeAmount = 0m; // TODO: Implement gateway fee calculation

            var revenueShare = new RevenueShare
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                PartnerId = sitePartner.PartnerId,
                PartnerShareAmount = partnerShareAmount,
                WebsiteShareAmount = websiteShareAmount,
                GatewayFeeAmount = gatewayFeeAmount,
                ComputedAt = DateTime.UtcNow
            };

            _context.RevenueShares.Add(revenueShare);
        }
    }
}
