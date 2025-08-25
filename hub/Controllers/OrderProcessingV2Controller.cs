using Microsoft.AspNetCore.Mvc;
using HubApi.Services;
using HubApi.Models;
using HubApi.Data;
using Microsoft.EntityFrameworkCore;

namespace HubApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class OrderProcessingV2Controller : ControllerBase
{
    private readonly OrderProcessingServiceV2 _orderProcessingService;
    private readonly OrderHubDbContext _context;
    private readonly ILogger<OrderProcessingV2Controller> _logger;

    public OrderProcessingV2Controller(OrderProcessingServiceV2 orderProcessingService, OrderHubDbContext context, ILogger<OrderProcessingV2Controller> logger)
    {
        _orderProcessingService = orderProcessingService;
        _context = context;
        _logger = logger;
    }

    [HttpPost("process/{rawOrderDataId:guid}")]
    public async Task<IActionResult> ProcessRawOrderData(Guid rawOrderDataId)
    {
        try
        {
            var rawOrderData = await _context.RawOrderData
                .FirstOrDefaultAsync(r => r.Id == rawOrderDataId);

            if (rawOrderData == null)
            {
                return NotFound(new { error = $"Raw order data with ID {rawOrderDataId} not found" });
            }

            var result = await _orderProcessingService.ProcessRawOrderDataAsync(rawOrderData);
            
            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing raw order data {RawOrderId}", rawOrderDataId);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            message = "Order Processing V2 service is running",
            timestamp = DateTime.UtcNow,
            version = "2.0"
        });
    }
}

