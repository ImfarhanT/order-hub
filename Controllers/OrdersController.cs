using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HubApi.Data;
using HubApi.Models;
using HubApi.DTOs;
using HubApi.Services;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;


namespace HubApi.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
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
                var query = _context.OrdersV2
                    .Include(o => o.Site)
                    .AsQueryable();

                // Apply filters
                if (siteId.HasValue)
                    query = query.Where(o => o.SiteId == siteId.Value);

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(o => o.Status == status);

                if (!string.IsNullOrEmpty(paymentGateway))
                    query = query.Where(o => o.PaymentGatewayCode == paymentGateway);

                // Note: Date filtering disabled for now since PlacedAt is stored as string
                // TODO: Implement proper date parsing for string dates
                // if (fromDate.HasValue)
                //     query = query.Where(o => DateTime.Parse(o.PlacedAt) >= fromDate.Value);

                // if (toDate.HasValue)
                //     query = query.Where(o => DateTime.Parse(o.PlacedAt) <= toDate.Value);

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
                    .OrderByDescending(o => o.SyncedAt) // Use SyncedAt for ordering since PlacedAt is string
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(o => new
                    {
                        o.Id,
                        o.WcOrderId,
                        o.SiteId,
                        SiteName = o.Site.Name,
                        o.CustomerName,
                        o.CustomerEmail,
                        o.OrderTotal,
                        o.Status,
                        o.PlacedAt,
                        o.SyncedAt,
                        ProfitCalculated = false // TODO: Check if profit exists in order_profits table
                    })
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
        /// Get orders for a specific partner (from their assigned sites)
        /// </summary>
        [HttpGet("partner/{partnerId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetOrdersForPartner(Guid partnerId)
        {
            try
            {
                // Get all site assignments for this partner
                var siteAssignments = await _context.SitePartners
                    .Where(sp => sp.PartnerId == partnerId && sp.IsActive)
                    .Select(sp => sp.SiteId)
                    .ToListAsync();

                if (!siteAssignments.Any())
                {
                    return Ok(new List<object>());
                }

                // Get orders from assigned sites (orders_v2 table)
                var orders = await _context.OrdersV2
                    .Where(o => siteAssignments.Contains(o.SiteId))
                    .Select(o => new
                    {
                        o.Id,
                        o.WcOrderId,
                        o.SiteId,
                        o.CustomerName,
                        o.CustomerEmail,
                        o.OrderTotal,
                        o.Status,
                        o.PlacedAt,
                        o.SyncedAt
                    })
                    .OrderByDescending(o => o.SyncedAt) // Use SyncedAt for ordering since PlacedAt is string
                    .ToListAsync();

                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders for partner {PartnerId}", partnerId);
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Test endpoint to verify routing
        /// </summary>
        [HttpPost("test")]
        public ActionResult<object> TestEndpoint()
        {
            return Ok(new { message = "POST endpoint is working!", timestamp = DateTime.UtcNow });
        }

        /// <summary>
        /// Accept raw JSON order data and store it for processing
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<object>> AcceptRawOrder([FromBody] object rawData)
        {
            try
            {
                // Get API key from header
                if (!Request.Headers.TryGetValue("X-API-Key", out var apiKeyValues) || !apiKeyValues.Any())
                {
                    return BadRequest(new { error = "X-API-Key header is required" });
                }

                var apiKey = apiKeyValues.First();
                _logger.LogInformation("Received raw order data with API key: {ApiKey}", apiKey);

                // Find the site by API key
                var site = await _context.Sites
                    .FirstOrDefaultAsync(s => s.ApiKey == apiKey && s.IsActive);

                if (site == null)
                {
                    _logger.LogWarning("Site not found for API key: {ApiKey}", apiKey);
                    return BadRequest(new { error = "Invalid API key" });
                }

                _logger.LogInformation("Processing raw order for site: {SiteName}", site.Name);

                // Create raw order data entry
                var rawOrderData = new RawOrderData
                {
                    Id = Guid.NewGuid(),
                    SiteId = site.Id,
                    SiteName = site.Name,
                    RawJson = JsonSerializer.Serialize(rawData),
                    ReceivedAt = DateTime.UtcNow,
                    Processed = false
                };

                _context.RawOrderData.Add(rawOrderData);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Raw order data saved with ID: {RawOrderId}", rawOrderData.Id);

                // Process the order automatically
                var orderProcessingService = HttpContext.RequestServices.GetRequiredService<IOrderProcessingService>();
                var result = await orderProcessingService.ProcessRawOrderDataAsync(rawOrderData.Id);

                if (result.Success)
                {
                    _logger.LogInformation("Order processed successfully: {OrderId}", result.ProcessedOrderId);
                    return Ok(new { 
                        success = true, 
                        message = "Order synced and processed successfully",
                        rawOrderId = rawOrderData.Id,
                        orderId = result.ProcessedOrderId
                    });
                }
                else
                {
                    _logger.LogWarning("Order processing failed: {Error}", result.Message);
                    return Ok(new { 
                        success = false, 
                        message = "Order synced but processing failed",
                        error = result.Message,
                        rawOrderId = rawOrderData.Id
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing raw order data");
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
                var query = _context.OrdersV2.AsQueryable();

                // Note: Date filtering disabled for now since PlacedAt is stored as string
                // TODO: Implement proper date parsing for string dates
                // if (fromDate.HasValue)
                //     query = query.Where(o => DateTime.Parse(o.PlacedAt) >= fromDate.Value);

                // if (toDate.HasValue)
                //     query = query.Where(o => DateTime.Parse(o.PlacedAt) <= toDate.Value);

                // Exclude cancelled and refunded orders from revenue calculations
                var revenueQuery = query.Where(o => o.Status.ToLower() != "cancelled" && o.Status.ToLower() != "refunded");

                var stats = await query
                    .GroupBy(o => o.SiteId)
                    .Select(g => new
                    {
                        site_id = g.Key,
                        total_orders = g.Count(),
                        total_revenue = revenueQuery.Where(o => o.SiteId == g.Key).Sum(o => decimal.Parse(o.OrderTotal)),
                        average_order_value = revenueQuery.Where(o => o.SiteId == g.Key).Average(o => decimal.Parse(o.OrderTotal))
                    })
                    .ToListAsync();

                var totalStats = new
                {
                    total_orders = await query.CountAsync(),
                    total_revenue = await revenueQuery.SumAsync(o => decimal.Parse(o.OrderTotal)),
                    average_order_value = await revenueQuery.AverageAsync(o => decimal.Parse(o.OrderTotal)),
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







    }
}
