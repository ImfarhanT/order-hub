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
    public class ShippingController : ControllerBase
    {
        private readonly OrderHubDbContext _context;
        private readonly ICryptoService _cryptoService;
        private readonly ILogger<ShippingController> _logger;

        public ShippingController(OrderHubDbContext context, ICryptoService cryptoService, ILogger<ShippingController> logger)
        {
            _context = context;
            _cryptoService = cryptoService;
            _logger = logger;
        }

        /// <summary>
        /// Update shipping status for an order
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateShipping([FromBody] ShippingUpdateRequest request)
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
                    string.IsNullOrEmpty(request.WcOrderId) ||
                    string.IsNullOrEmpty(request.Status))
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
                var signatureBase = $"{request.SiteApiKey}|{request.Timestamp}|{request.Nonce}|{request.WcOrderId}|0";
                var expectedSignature = ComputeHmacSignature(signatureBase, _cryptoService.Decrypt(site.ApiSecretEnc));

                if (request.Signature != expectedSignature)
                {
                    return Unauthorized(new { error = "Invalid signature" });
                }

                // Find the order
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.SiteId == site.Id && o.WcOrderId == request.WcOrderId);

                if (order == null)
                {
                    return NotFound(new { error = "Order not found" });
                }

                // Create shipping update
                var shippingUpdate = new ShippingUpdate
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    Status = request.Status,
                    Provider = request.Provider ?? "",
                    TrackingNumber = request.TrackingNumber ?? "",
                    Payload = JsonDocument.Parse(JsonSerializer.Serialize(request.Payload ?? new object())),
                    OccurredAt = request.OccurredAt
                };

                _context.ShippingUpdates.Add(shippingUpdate);

                // Update order status if this is a terminal status
                if (IsTerminalStatus(request.Status))
                {
                    order.Status = request.Status;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Shipping update processed successfully: {OrderId} - {Status}", 
                    order.WcOrderId, request.Status);

                return Ok(new { ok = true, shipping_update_id = shippingUpdate.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing shipping update");
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Get shipping updates for an order
        /// </summary>
        [HttpGet("order/{orderId}")]
        public async Task<ActionResult<IEnumerable<ShippingUpdate>>> GetShippingUpdates(Guid orderId)
        {
            try
            {
                var shippingUpdates = await _context.ShippingUpdates
                    .Where(su => su.OrderId == orderId)
                    .OrderByDescending(su => su.OccurredAt)
                    .ToListAsync();

                return Ok(shippingUpdates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving shipping updates for order {OrderId}", orderId);
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Get shipping statistics
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetShippingStats(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var query = _context.ShippingUpdates.AsQueryable();

                if (fromDate.HasValue)
                    query = query.Where(su => su.OccurredAt >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(su => su.OccurredAt <= toDate.Value);

                var stats = await query
                    .GroupBy(su => su.Status)
                    .Select(g => new
                    {
                        status = g.Key,
                        count = g.Count()
                    })
                    .ToListAsync();

                var totalUpdates = await query.CountAsync();
                var totalOrders = await query.Select(su => su.OrderId).Distinct().CountAsync();

                var result = new
                {
                    total_updates = totalUpdates,
                    total_orders_updated = totalOrders,
                    by_status = stats
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving shipping statistics");
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete a shipping update
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteShippingUpdate(Guid id)
        {
            try
            {
                var shippingUpdate = await _context.ShippingUpdates.FindAsync(id);
                if (shippingUpdate == null)
                {
                    return NotFound(new { error = "Shipping update not found" });
                }

                _context.ShippingUpdates.Remove(shippingUpdate);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Shipping update deleted: {Id}", id);
                return Ok(new { ok = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting shipping update {Id}", id);
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
        /// Check if a status is terminal (final)
        /// </summary>
        private bool IsTerminalStatus(string status)
        {
            var terminalStatuses = new[] { "delivered", "failed", "cancelled", "refunded" };
            return terminalStatuses.Contains(status.ToLower());
        }
    }
}
