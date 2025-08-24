using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HubApi.Data;
using HubApi.Models;
using HubApi.DTOs;
using HubApi.Services;
using System.Text.Json;

namespace HubApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly OrderHubDbContext _context;
        private readonly ICryptoService _cryptoService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(OrderHubDbContext context, ICryptoService cryptoService, ILogger<OrdersController> logger)
        {
            _context = context;
            _cryptoService = cryptoService;
            _logger = logger;
        }

        /// <summary>
        /// Get all orders with optional filtering
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders(
            [FromQuery] Guid? siteId = null,
            [FromQuery] string? status = null,
            [FromQuery] string? paymentGateway = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var query = _context.Orders
                    .Include(o => o.Site)
                    .Include(o => o.OrderItems)
                    .AsQueryable();

                // Apply filters
                if (siteId.HasValue)
                    query = query.Where(o => o.SiteId == siteId.Value);

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(o => o.Status == status);

                if (!string.IsNullOrEmpty(paymentGateway))
                    query = query.Where(o => o.PaymentGatewayCode == paymentGateway);

                if (fromDate.HasValue)
                    query = query.Where(o => o.PlacedAt >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(o => o.PlacedAt <= toDate.Value);

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(o => 
                        o.WcOrderId.Contains(search) || 
                        o.CustomerName.Contains(search) || 
                        o.CustomerEmail.Contains(search));
                }

                // Apply pagination
                var totalCount = await query.CountAsync();
                var orders = await query
                    .OrderByDescending(o => o.PlacedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                Response.Headers.Add("X-Total-Count", totalCount.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());

                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders");
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Get a specific order by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(Guid id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Site)
                    .Include(o => o.OrderItems)
                    .Include(o => o.ShippingUpdates)
                    .Include(o => o.RevenueShares)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    return NotFound(new { error = "Order not found" });
                }

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order {OrderId}", id);
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Synchronize order from WooCommerce plugin
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SyncOrder([FromBody] OrderSyncRequest request)
        {
            try
            {
                // Validate request
                if (request == null)
                {
                    return BadRequest(new { error = "Request body is required" });
                }

                // Validate required fields
                if (string.IsNullOrEmpty(request.SiteApiKey) || 
                    string.IsNullOrEmpty(request.Nonce) || 
                    request.Timestamp == 0 || 
                    string.IsNullOrEmpty(request.Signature) ||
                    request.Order == null || 
                    request.Items == null)
                {
                    return BadRequest(new { error = "Missing required fields" });
                }

                // Find site by API key
                var site = await _context.Sites
                    .FirstOrDefaultAsync(s => s.ApiKey == request.SiteApiKey && s.IsActive);

                if (site == null)
                {
                    return Unauthorized(new { error = "Invalid API key or site inactive" });
                }

                // Validate timestamp (within 10 minutes)
                var requestTime = DateTimeOffset.FromUnixTimeSeconds(request.Timestamp);
                var currentTime = DateTimeOffset.UtcNow;
                if (Math.Abs((currentTime - requestTime).TotalMinutes) > 10)
                {
                    return Unauthorized(new { error = "Request timestamp is too old or too new" });
                }

                // Check nonce replay
                var existingNonce = await _context.RequestNonces
                    .FirstOrDefaultAsync(n => n.SiteId == site.Id && n.Nonce == request.Nonce);

                if (existingNonce != null)
                {
                    return Unauthorized(new { error = "Nonce already used" });
                }

                // Store nonce to prevent replay
                var nonce = new RequestNonce
                {
                    SiteId = site.Id,
                    Nonce = request.Nonce,
                    Timestamp = requestTime.UtcDateTime,
                    Expires = requestTime.UtcDateTime.AddMinutes(15)
                };
                _context.RequestNonces.Add(nonce);

                // Validate signature
                var signatureBase = $"{request.SiteApiKey}|{request.Timestamp}|{request.Nonce}|{request.Order.WcOrderId}|{request.Order.OrderTotal}";
                var expectedSignature = ComputeHmacSignature(signatureBase, _cryptoService.Decrypt(site.ApiSecretEnc));

                if (request.Signature != expectedSignature)
                {
                    return Unauthorized(new { error = "Invalid signature" });
                }

                // Find existing order or create new one
                var existingOrder = await _context.Orders
                    .FirstOrDefaultAsync(o => o.SiteId == site.Id && o.WcOrderId == request.Order.WcOrderId);

                Order order;
                if (existingOrder != null)
                {
                    // Update existing order
                    order = existingOrder;
                    order.Status = request.Order.Status;
                    order.OrderTotal = request.Order.OrderTotal;
                    order.Subtotal = request.Order.Subtotal;
                    order.DiscountTotal = request.Order.DiscountTotal;
                    order.ShippingTotal = request.Order.ShippingTotal;
                    order.TaxTotal = request.Order.TaxTotal;
                    order.PaymentGatewayCode = request.Order.PaymentGatewayCode;
                    order.CustomerName = request.Order.CustomerName;
                    order.CustomerEmail = request.Order.CustomerEmail;
                    order.CustomerPhone = request.Order.CustomerPhone;
                    order.ShippingAddress = JsonDocument.Parse(JsonSerializer.Serialize(request.Order.ShippingAddress));
                    order.BillingAddress = JsonDocument.Parse(JsonSerializer.Serialize(request.Order.BillingAddress));
                }
                else
                {
                    // Create new order
                    order = new Order
                    {
                        Id = Guid.NewGuid(),
                        SiteId = site.Id,
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
                        ShippingAddress = JsonDocument.Parse(JsonSerializer.Serialize(request.Order.ShippingAddress)),
                        BillingAddress = JsonDocument.Parse(JsonSerializer.Serialize(request.Order.BillingAddress)),
                        PlacedAt = request.Order.PlacedAt,
                        SyncedAt = DateTime.UtcNow
                    };
                    _context.Orders.Add(order);
                }

                // Add order items
                foreach (var itemRequest in request.Items)
                {
                    var orderItem = new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        ProductId = itemRequest.ProductId,
                        Sku = itemRequest.Sku,
                        Name = itemRequest.Name,
                        Qty = itemRequest.Qty,
                        Price = itemRequest.Price,
                        Subtotal = itemRequest.Subtotal,
                        Total = itemRequest.Total
                    };
                    _context.OrderItems.Add(orderItem);
                }

                // Calculate and store revenue shares
                await CalculateRevenueShares(order, site, request.GatewayFeePercent, request.GatewayFeeAmount);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Order synchronized successfully: {OrderId} from site {SiteId}", 
                    order.WcOrderId, site.Id);

                return Ok(new { ok = true, order_id = order.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error synchronizing order");
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Bulk sync multiple orders
        /// </summary>
        [HttpPost("bulk")]
        public async Task<IActionResult> BulkSyncOrders([FromBody] List<OrderSyncRequest> requests)
        {
            try
            {
                if (requests == null || !requests.Any())
                {
                    return BadRequest(new { error = "Request body must contain at least one order" });
                }

                var results = new List<object>();
                var successCount = 0;
                var errorCount = 0;

                foreach (var request in requests)
                {
                    try
                    {
                        var result = await SyncOrder(request);
                        if (result is OkObjectResult)
                        {
                            successCount++;
                            results.Add(new { order_id = request.Order.WcOrderId, status = "success" });
                        }
                        else
                        {
                            errorCount++;
                            results.Add(new { order_id = request.Order.WcOrderId, status = "error", message = "Failed to sync" });
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        results.Add(new { order_id = request.Order.WcOrderId, status = "error", message = ex.Message });
                    }
                }

                return Ok(new 
                { 
                    ok = true, 
                    summary = new { total = requests.Count, success = successCount, errors = errorCount },
                    results = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk order sync");
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete an order
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(Guid id)
        {
            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null)
                {
                    return NotFound(new { error = "Order not found" });
                }

                // Remove related data
                var orderItems = await _context.OrderItems.Where(oi => oi.OrderId == id).ToListAsync();
                var shippingUpdates = await _context.ShippingUpdates.Where(su => su.OrderId == id).ToListAsync();
                var revenueShares = await _context.RevenueShares.Where(rs => rs.OrderId == id).ToListAsync();

                _context.OrderItems.RemoveRange(orderItems);
                _context.ShippingUpdates.RemoveRange(shippingUpdates);
                _context.RevenueShares.RemoveRange(revenueShares);
                _context.Orders.Remove(order);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Order deleted: {OrderId}", id);
                return Ok(new { ok = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order {OrderId}", id);
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Get order statistics
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetOrderStats(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var query = _context.Orders.AsQueryable();

                if (fromDate.HasValue)
                    query = query.Where(o => o.PlacedAt >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(o => o.PlacedAt <= toDate.Value);

                var stats = await query
                    .GroupBy(o => o.SiteId)
                    .Select(g => new
                    {
                        site_id = g.Key,
                        total_orders = g.Count(),
                        total_revenue = g.Sum(o => o.OrderTotal),
                        average_order_value = g.Average(o => o.OrderTotal)
                    })
                    .ToListAsync();

                var totalStats = new
                {
                    total_orders = await query.CountAsync(),
                    total_revenue = await query.SumAsync(o => o.OrderTotal),
                    average_order_value = await query.AverageAsync(o => o.OrderTotal),
                    by_site = stats
                };

                return Ok(totalStats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order statistics");
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Compute HMAC signature
        /// </summary>
        private string ComputeHmacSignature(string data, string secret)
        {
            using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret));
            var hashBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// Calculate revenue shares for an order
        /// </summary>
        private async Task CalculateRevenueShares(Order order, Site site, decimal? gatewayFeePercent = null, decimal? gatewayFeeAmount = null)
        {
            try
            {
                // Get site partners
                var sitePartners = await _context.SitePartners
                    .Include(sp => sp.Partner)
                    .Where(sp => sp.SiteId == site.Id)
                    .ToListAsync();

                // Get site gateways
                var siteGateways = await _context.SiteGateways
                    .Include(sg => sg.Gateway)
                    .Where(sg => sg.SiteId == site.Id)
                    .ToListAsync();

                // Calculate gateway fee
                decimal gatewayFee = 0;
                if (gatewayFeeAmount.HasValue)
                {
                    gatewayFee = gatewayFeeAmount.Value;
                }
                else if (gatewayFeePercent.HasValue)
                {
                    gatewayFee = order.OrderTotal * (gatewayFeePercent.Value / 100);
                }
                else
                {
                    // Get from site gateways
                    var siteGateway = siteGateways.FirstOrDefault(sg => sg.Gateway.Code == order.PaymentGatewayCode);
                    if (siteGateway != null)
                    {
                        gatewayFee = order.OrderTotal * (siteGateway.WebsiteSharePercent / 100);
                    }
                }

                // Calculate partner shares
                foreach (var sitePartner in sitePartners)
                {
                    var partnerShare = order.OrderTotal * (sitePartner.SharePercent / 100);
                    var websiteShare = order.OrderTotal - partnerShare - gatewayFee;

                    var revenueShare = new RevenueShare
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        PartnerId = sitePartner.PartnerId,
                        PartnerShareAmount = partnerShare,
                        WebsiteShareAmount = websiteShare,
                        GatewayFeeAmount = gatewayFee,
                        ComputedAt = DateTime.UtcNow
                    };

                    _context.RevenueShares.Add(revenueShare);
                }

                // If no partners, create website-only share
                if (!sitePartners.Any())
                {
                    var websiteShare = new RevenueShare
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        PartnerId = Guid.Empty, // Use empty GUID instead of null
                        PartnerShareAmount = 0,
                        WebsiteShareAmount = order.OrderTotal - gatewayFee,
                        GatewayFeeAmount = gatewayFee,
                        ComputedAt = DateTime.UtcNow
                    };

                    _context.RevenueShares.Add(websiteShare);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating revenue shares for order {OrderId}", order.Id);
                // Don't throw - revenue share calculation failure shouldn't break order sync
            }
        }
    }
}
