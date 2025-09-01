using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HubApi.Data;
using HubApi.Models;
using HubApi.DTOs;
using HubApi.Services;

namespace HubApi.Controllers;

[ApiController]
[Route("api/v1/shipments")]
[Authorize]
public class ShipmentController : ControllerBase
{
    private readonly OrderHubDbContext _context;
    private readonly ILogger<ShipmentController> _logger;
    private readonly ITrackingService _trackingService;

    public ShipmentController(OrderHubDbContext context, ILogger<ShipmentController> logger, ITrackingService trackingService)
    {
        _context = context;
        _logger = logger;
        _trackingService = trackingService;
    }

    /// <summary>
    /// Get all shipments with order details
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<object>> GetShipments([FromQuery] string? status = null)
    {
        try
        {
            _logger.LogInformation("Getting shipments with status filter: {Status}", status ?? "all");
            
            var query = _context.Shipments
                .Include(s => s.Order)
                .ThenInclude(o => o.OrderItems)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(s => s.Status.ToLower() == status.ToLower());
            }

            var shipments = await query
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
            
            _logger.LogInformation("Found {Count} shipments", shipments.Count);

            var response = shipments.Select(s => new ShipmentResponse
            {
                Id = s.Id,
                OrderId = s.OrderId,
                OrderNumber = s.Order?.WcOrderId ?? "Unknown",
                CustomerName = s.Order?.CustomerName ?? "Unknown",
                CustomerEmail = s.Order?.CustomerEmail ?? "Unknown",
                CustomerPhone = s.Order?.CustomerPhone ?? "Unknown",
                ShippingAddress = s.Order?.ShippingAddress?.RootElement.GetRawText() ?? "Address not available",
                TrackingNumber = s.TrackingNumber,
                Carrier = s.Carrier,
                Status = s.Status,
                TrackingUrl = s.TrackingUrl,
                ShippedAt = s.ShippedAt,
                EstimatedDelivery = s.EstimatedDelivery,
                DeliveredAt = s.DeliveredAt,
                Notes = s.Notes,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                OrderItems = s.Order?.OrderItems?.Select(oi => new
                {
                    Name = oi.Name ?? "Unknown Product",
                    Qty = oi.Qty,
                    Total = oi.Total
                }).Cast<object>().ToList() ?? new List<object>()
            }).ToList();

            return Ok(new
            {
                success = true,
                data = response,
                total = response.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get shipments: {Message}", ex.Message);
            return StatusCode(500, new { 
                success = false, 
                message = ex.Message,
                exceptionType = ex.GetType().Name,
                details = ex.ToString()
            });
        }
    }

    /// <summary>
    /// Get all shipped orders (including those without tracking numbers)
    /// </summary>
    [HttpGet("all-shipped-orders")]
    public async Task<ActionResult<object>> GetAllShippedOrders()
    {
        try
        {
            _logger.LogInformation("Getting all shipped orders (including those without tracking)");
            
            // Step 1: Get all orders with shipped status
            _logger.LogInformation("Step 1: Getting all shipped orders...");
            var shippedOrders = await _context.OrdersV2
                .Include(o => o.OrderItems)
                .Where(o => o.Status.ToLower() == "shipped")
                .ToListAsync();
            _logger.LogInformation("Total shipped orders: {Count}", shippedOrders.Count);
            
            if (shippedOrders.Count == 0)
            {
                return Ok(new { success = true, data = new List<object>(), total = 0 });
            }
            
            // Step 2: Get existing shipments
            _logger.LogInformation("Step 2: Getting existing shipments...");
            var shipments = await _context.Shipments.ToListAsync();
            _logger.LogInformation("Total shipments: {Count}", shipments.Count);
            
            // Step 3: Create response combining orders with and without tracking
            _logger.LogInformation("Step 3: Creating combined response...");
            var response = shippedOrders.Select(o => 
            {
                var existingShipment = shipments.FirstOrDefault(s => s.OrderId == o.Id);
                
                return new
                {
                    o.Id,
                    o.WcOrderId,
                    o.CustomerName,
                    o.CustomerEmail,
                    o.CustomerPhone,
                    o.ShippingAddress,
                    o.OrderTotal,
                    o.Status,
                    o.SyncedAt,
                    // Shipment info (if exists)
                    HasTracking = existingShipment != null,
                    TrackingNumber = existingShipment?.TrackingNumber ?? "",
                    Carrier = existingShipment?.Carrier ?? "",
                    ShipmentStatus = existingShipment?.Status ?? "no-tracking",
                    ShipmentId = existingShipment?.Id,
                    // Order items
                    OrderItems = o.OrderItems?.Select(oi => new
                    {
                        Name = oi.Name ?? "Unknown Product",
                        Qty = oi.Qty,
                        Total = oi.Total
                    }).Cast<object>().ToList() ?? new List<object>()
                };
            }).OrderByDescending(o => o.SyncedAt).ToList();

            _logger.LogInformation("Step 4: Returning response with {Count} shipped orders", response.Count);
            return Ok(new
            {
                success = true,
                data = response,
                total = response.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all shipped orders: {Message}", ex.Message);
            return StatusCode(500, new { 
                success = false, 
                message = ex.Message,
                exceptionType = ex.GetType().Name,
                details = ex.ToString()
            });
        }
    }

    /// <summary>
    /// Get orders that need shipping (no shipment created yet)
    /// </summary>
    [HttpGet("pending-orders")]
    public async Task<ActionResult<object>> GetPendingOrders()
    {
        try
        {
            _logger.LogInformation("Getting pending orders that need shipping");
            
            // Step 1: Get all orders
            _logger.LogInformation("Step 1: Getting all orders...");
            var allOrders = await _context.OrdersV2.ToListAsync();
            _logger.LogInformation("Total orders in database: {Count}", allOrders.Count);
            
            if (allOrders.Count == 0)
            {
                return Ok(new { success = true, data = new List<object>(), total = 0 });
            }
            
            // Step 2: Filter out completed/cancelled orders
            _logger.LogInformation("Step 2: Filtering out completed/cancelled orders...");
            var validOrders = allOrders.Where(o => 
                o.Status.ToLower() != "cancelled" && 
                o.Status.ToLower() != "refunded" &&
                o.Status.ToLower() != "shipped" &&
                o.Status.ToLower() != "delivered" &&
                o.Status.ToLower() != "completed" &&
                o.Status.ToLower() != "fulfilled").ToList();
            _logger.LogInformation("Valid orders (not completed/cancelled): {Count}", validOrders.Count);
            
            // Step 3: Get shipments
            _logger.LogInformation("Step 3: Getting shipments...");
            var shipments = await _context.Shipments.ToListAsync();
            _logger.LogInformation("Total shipments: {Count}", shipments.Count);
            
            // Step 4: Filter out orders that already have shipments
            _logger.LogInformation("Step 4: Filtering orders without shipments...");
            var pendingOrders = validOrders.Where(o => 
                !shipments.Any(s => s.OrderId == o.Id)).ToList();
            _logger.LogInformation("Pending orders (need shipping): {Count}", pendingOrders.Count);
            
            // Step 5: Order by date
            _logger.LogInformation("Step 5: Ordering by date...");
            pendingOrders = pendingOrders.OrderByDescending(o => o.SyncedAt).ToList();

            // Step 6: Create response
            _logger.LogInformation("Step 6: Creating response...");
            var response = pendingOrders.Select(o => new
            {
                o.Id,
                o.WcOrderId,
                o.CustomerName,
                o.CustomerEmail,
                o.CustomerPhone,
                o.ShippingAddress,
                o.OrderTotal,
                o.Status,
                o.SyncedAt,
                OrderItems = new List<object>()
            }).ToList();

            _logger.LogInformation("Step 7: Returning response with {Count} orders", response.Count);
            return Ok(new
            {
                success = true,
                data = response,
                total = response.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pending orders: {Message}", ex.Message);
            return StatusCode(500, new { 
                success = false, 
                message = ex.Message,
                exceptionType = ex.GetType().Name,
                details = ex.ToString()
            });
        }
    }

    /// <summary>
    /// Create a new shipment
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<object>> CreateShipment([FromBody] CreateShipmentRequest request)
    {
        try
        {
            // Check if order exists and doesn't already have a shipment
            var order = await _context.OrdersV2.FindAsync(request.OrderId);
            if (order == null)
            {
                return BadRequest(new { success = false, message = "Order not found" });
            }

            var existingShipment = await _context.Shipments.FirstOrDefaultAsync(s => s.OrderId == request.OrderId);
            if (existingShipment != null)
            {
                return BadRequest(new { success = false, message = "Order already has a shipment" });
            }

            var shipment = new Shipment
            {
                OrderId = request.OrderId,
                TrackingNumber = request.TrackingNumber,
                Carrier = request.Carrier,
                TrackingUrl = request.TrackingUrl,
                EstimatedDelivery = request.EstimatedDelivery,
                Notes = request.Notes,
                Status = "pending"
            };

            _context.Shipments.Add(shipment);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Shipment created successfully",
                data = new { shipment.Id, shipment.TrackingNumber }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create shipment: {Message}", ex.Message);
            return StatusCode(500, new { 
                success = false, 
                message = ex.Message,
                exceptionType = ex.GetType().Name,
                details = ex.ToString()
            });
        }
    }

    /// <summary>
    /// Update shipment status and details
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<object>> UpdateShipment(Guid id, [FromBody] UpdateShipmentRequest request)
    {
        try
        {
            var shipment = await _context.Shipments.FindAsync(id);
            if (shipment == null)
            {
                return NotFound(new { success = false, message = "Shipment not found" });
            }

            shipment.TrackingNumber = request.TrackingNumber;
            shipment.Carrier = request.Carrier;
            shipment.Status = request.Status;
            shipment.TrackingUrl = request.TrackingUrl;
            shipment.EstimatedDelivery = request.EstimatedDelivery;
            shipment.Notes = request.Notes;
            shipment.UpdatedAt = DateTime.UtcNow;

            // Update timestamps based on status
            if (request.Status.ToLower() == "shipped" && !shipment.ShippedAt.HasValue)
            {
                shipment.ShippedAt = DateTime.UtcNow;
            }
            else if (request.Status.ToLower() == "delivered" && !shipment.DeliveredAt.HasValue)
            {
                shipment.DeliveredAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Shipment updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update shipment: {Message}", ex.Message);
            return StatusCode(500, new { 
                success = false, 
                message = ex.Message,
                exceptionType = ex.GetType().Name,
                details = ex.ToString()
            });
        }
    }

    /// <summary>
    /// Simple test endpoint to check database connection
    /// </summary>
    [HttpGet("test")]
    public async Task<ActionResult<object>> TestEndpoint()
    {
        try
        {
            _logger.LogInformation("Testing database connection...");
            
            // Test 1: Count orders
            var orderCount = await _context.OrdersV2.CountAsync();
            _logger.LogInformation("Orders count: {Count}", orderCount);
            
            // Test 2: Count shipments
            var shipmentCount = await _context.Shipments.CountAsync();
            _logger.LogInformation("Shipments count: {Count}", shipmentCount);
            
            // Test 3: Get one order
            var firstOrder = await _context.OrdersV2.FirstOrDefaultAsync();
            
            return Ok(new
            {
                success = true,
                message = "Database connection test successful",
                data = new
                {
                    orderCount,
                    shipmentCount,
                    firstOrderId = firstOrder?.Id,
                    firstOrderStatus = firstOrder?.Status
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection test failed");
            return StatusCode(500, new { success = false, message = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// Get shipment statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<object>> GetShipmentStatistics()
    {
        try
        {
            var totalShipments = await _context.Shipments.CountAsync();
            var pendingShipments = await _context.Shipments.CountAsync(s => s.Status.ToLower() == "pending");
            var shippedShipments = await _context.Shipments.CountAsync(s => s.Status.ToLower() == "shipped");
            var deliveredShipments = await _context.Shipments.CountAsync(s => s.Status.ToLower() == "delivered");
            var exceptionShipments = await _context.Shipments.CountAsync(s => s.Status.ToLower() == "exception");

            var pendingOrders = await _context.OrdersV2
                .Where(o => o.Status.ToLower() != "cancelled" && 
                           o.Status.ToLower() != "refunded" &&
                           o.Status.ToLower() != "shipped" &&
                           o.Status.ToLower() != "delivered" &&
                           o.Status.ToLower() != "completed" &&
                           o.Status.ToLower() != "fulfilled" &&
                           !_context.Shipments.Any(s => s.OrderId == o.Id))
                .CountAsync();

            return Ok(new
            {
                success = true,
                data = new
                {
                    totalShipments,
                    pendingShipments,
                    shippedShipments,
                    deliveredShipments,
                    exceptionShipments,
                    pendingOrders
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get shipment statistics: {Message}", ex.Message);
            return StatusCode(500, new { 
                success = false, 
                message = ex.Message,
                exceptionType = ex.GetType().Name,
                details = ex.ToString()
            });
        }
    }

    /// <summary>
    /// Test endpoint to check AfterShip API directly
    /// </summary>
    [HttpGet("test-aftership")]
    public async Task<ActionResult<object>> TestAfterShip([FromQuery] string trackingNumber)
    {
        try
        {
            if (string.IsNullOrEmpty(trackingNumber))
            {
                return BadRequest(new { success = false, message = "Tracking number is required" });
            }

            _logger.LogInformation("Testing AfterShip API with tracking number: {TrackingNumber}", trackingNumber);

            // Test the AfterShip API directly
            var liveTracking = await _trackingService.GetLiveTrackingAsync(trackingNumber, "");

            return Ok(new
            {
                success = true,
                message = "AfterShip API test successful",
                trackingNumber,
                liveTracking
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AfterShip API test failed: {Message}", ex.Message);
            return StatusCode(500, new { 
                success = false, 
                message = ex.Message,
                exceptionType = ex.GetType().Name,
                details = ex.ToString()
            });
        }
    }

    /// <summary>
    /// Get live tracking information for a shipment
    /// </summary>
    [HttpGet("{id}/live-tracking")]
    public async Task<ActionResult<object>> GetLiveTracking(Guid id)
    {
        try
        {
            var shipment = await _context.Shipments
                .Include(s => s.Order)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (shipment == null)
            {
                return NotFound(new { success = false, message = "Shipment not found" });
            }

            // Get live tracking from 17track
            var liveTracking = await _trackingService.GetLiveTrackingAsync(
                shipment.TrackingNumber, 
                shipment.Carrier);

            // Update shipment status if it changed
            if (liveTracking.Status != shipment.Status)
            {
                shipment.Status = liveTracking.Status;
                shipment.UpdatedAt = DateTime.UtcNow;
                
                if (liveTracking.IsDelivered)
                {
                    shipment.DeliveredAt = liveTracking.DeliveredAt;
                }
                
                if (liveTracking.EstimatedDelivery.HasValue)
                {
                    shipment.EstimatedDelivery = liveTracking.EstimatedDelivery;
                }
                
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    shipment = new
                    {
                        shipment.Id,
                        shipment.TrackingNumber,
                        shipment.Carrier,
                        shipment.Status,
                        shipment.EstimatedDelivery,
                        shipment.DeliveredAt
                    },
                    liveTracking
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get live tracking: {Message}", ex.Message);
            return StatusCode(500, new { 
                success = false, 
                message = ex.Message,
                exceptionType = ex.GetType().Name,
                details = ex.ToString()
            });
        }
    }

    /// <summary>
    /// Refresh all shipment tracking data
    /// </summary>
    [HttpPost("refresh-tracking")]
    public async Task<ActionResult<object>> RefreshAllTracking()
    {
        try
        {
            var shipments = await _context.Shipments
                .Where(s => !string.IsNullOrEmpty(s.TrackingNumber))
                .ToListAsync();

            var results = new List<object>();
            var updatedCount = 0;

            foreach (var shipment in shipments)
            {
                try
                {
                    var liveTracking = await _trackingService.GetLiveTrackingAsync(
                        shipment.TrackingNumber, 
                        shipment.Carrier);

                    if (liveTracking.Status != shipment.Status)
                    {
                        shipment.Status = liveTracking.Status;
                        shipment.UpdatedAt = DateTime.UtcNow;
                        
                        if (liveTracking.IsDelivered)
                        {
                            shipment.DeliveredAt = liveTracking.DeliveredAt;
                        }
                        
                        if (liveTracking.EstimatedDelivery.HasValue)
                        {
                            shipment.EstimatedDelivery = liveTracking.EstimatedDelivery;
                        }
                        
                        updatedCount++;
                    }

                    results.Add(new
                    {
                        shipment.Id,
                        shipment.TrackingNumber,
                        oldStatus = shipment.Status,
                        newStatus = liveTracking.Status,
                        updated = liveTracking.Status != shipment.Status
                    });

                    // Rate limiting - 17track allows 100 requests per minute
                    await Task.Delay(600);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error refreshing tracking for shipment {Id}", shipment.Id);
                    results.Add(new
                    {
                        shipment.Id,
                        shipment.TrackingNumber,
                        error = ex.Message
                    });
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = $"Refreshed {shipments.Count} shipments, updated {updatedCount}",
                data = results
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh tracking: {Message}", ex.Message);
            return StatusCode(500, new { 
                success = false, 
                message = ex.Message,
                exceptionType = ex.GetType().Name,
                details = ex.ToString()
            });
        }
    }

    /// <summary>
    /// Delete shipment
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<object>> DeleteShipment(Guid id)
    {
        try
        {
            var shipment = await _context.Shipments.FindAsync(id);
            if (shipment == null)
            {
                return NotFound(new { success = false, message = "Shipment not found" });
            }

            _context.Shipments.Remove(shipment);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Shipment deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete shipment: {Message}", ex.Message);
            return StatusCode(500, new { 
                success = false, 
                message = ex.Message,
                exceptionType = ex.GetType().Name,
                details = ex.ToString()
            });
        }
    }
}
