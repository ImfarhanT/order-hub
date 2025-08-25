using Microsoft.AspNetCore.Mvc;
using HubApi.Services;
using HubApi.Models;

namespace HubApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class OrderProcessingController : ControllerBase
{
    private readonly IOrderProcessingService _orderProcessingService;
    private readonly ILogger<OrderProcessingController> _logger;

    public OrderProcessingController(IOrderProcessingService orderProcessingService, ILogger<OrderProcessingController> logger)
    {
        _orderProcessingService = orderProcessingService;
        _logger = logger;
    }

    [HttpPost("process/{rawOrderDataId:guid}")]
    public async Task<IActionResult> ProcessRawOrderData(Guid rawOrderDataId)
    {
        try
        {
            var result = await _orderProcessingService.ProcessRawOrderDataAsync(rawOrderDataId);
            
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

    [HttpPost("process-all")]
    public async Task<IActionResult> ProcessAllUnprocessedRawData()
    {
        try
        {
            var results = await _orderProcessingService.ProcessAllUnprocessedRawDataAsync();
            
            var successCount = results.Count(r => r.Success);
            var failureCount = results.Count(r => !r.Success);
            
            return Ok(new
            {
                message = $"Processed {results.Count} raw order data entries",
                success_count = successCount,
                failure_count = failureCount,
                results = results
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing all unprocessed raw order data");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetProcessingStatus()
    {
        try
        {
            // This would need to be implemented in the service
            // For now, return basic info
            return Ok(new
            {
                message = "Order processing service is running",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting processing status");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }
}

