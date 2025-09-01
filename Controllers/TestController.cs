using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HubApi.Data;
using HubApi.Models;

namespace HubApi.Controllers;

[ApiController]
[Route("api/v1/test")]
public class TestController : ControllerBase
{
    private readonly OrderHubDbContext _context;
    private readonly ILogger<TestController> _logger;

    public TestController(OrderHubDbContext context, ILogger<TestController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("db-connection")]
    public async Task<ActionResult<object>> TestDatabaseConnection()
    {
        try
        {
            _logger.LogInformation("Testing basic database connection...");
            
            // Test 1: Simple count
            var orderCount = await _context.OrdersV2.CountAsync();
            _logger.LogInformation("Orders count: {Count}", orderCount);
            
            return Ok(new
            {
                success = true,
                message = "Database connection successful",
                orderCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection test failed");
            return StatusCode(500, new 
            { 
                success = false, 
                message = ex.Message,
                exceptionType = ex.GetType().Name
            });
        }
    }

    [HttpGet("orders-simple")]
    public async Task<ActionResult<object>> GetOrdersSimple()
    {
        try
        {
            _logger.LogInformation("Getting orders with simple query...");
            
            var orders = await _context.OrdersV2
                .Select(o => new
                {
                    o.Id,
                    o.WcOrderId,
                    o.Status,
                    o.CustomerName
                })
                .Take(5)
                .ToListAsync();
            
            _logger.LogInformation("Retrieved {Count} orders", orders.Count);
            
            return Ok(new
            {
                success = true,
                data = orders
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Simple orders query failed");
            return StatusCode(500, new 
            { 
                success = false, 
                message = ex.Message,
                exceptionType = ex.GetType().Name
            });
        }
    }

    [HttpGet("shipments-simple")]
    public async Task<ActionResult<object>> GetShipmentsSimple()
    {
        try
        {
            _logger.LogInformation("Getting shipments with simple query...");
            
            var shipments = await _context.Shipments
                .Select(s => new
                {
                    s.Id,
                    s.OrderId,
                    s.TrackingNumber,
                    s.Status
                })
                .ToListAsync();
            
            _logger.LogInformation("Retrieved {Count} shipments", shipments.Count);
            
            return Ok(new
            {
                success = true,
                data = shipments
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Simple shipments query failed");
            return StatusCode(500, new 
            { 
                success = false, 
                message = ex.Message,
                exceptionType = ex.GetType().Name
            });
        }
    }
}


