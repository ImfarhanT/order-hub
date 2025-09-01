using Microsoft.AspNetCore.Mvc;
using HubApi.Services;
using Microsoft.Extensions.Logging;

namespace HubApi.Controllers;

[ApiController]
[Route("api/v1/test-tracking")]
public class TestTrackingController : ControllerBase
{
    private readonly ITrackingService _trackingService;
    private readonly ILogger<TestTrackingController> _logger;

    public TestTrackingController(ITrackingService trackingService, ILogger<TestTrackingController> logger)
    {
        _trackingService = trackingService;
        _logger = logger;
    }

    /// <summary>
    /// Test AfterShip API directly - No authentication required
    /// </summary>
    [HttpGet("aftership")]
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
                liveTracking,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AfterShip API test failed: {Message}", ex.Message);
            return StatusCode(500, new { 
                success = false, 
                message = ex.Message,
                exceptionType = ex.GetType().Name,
                details = ex.ToString(),
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Test if the service is working - No authentication required
    /// </summary>
    [HttpGet("health")]
    public ActionResult<object> HealthCheck()
    {
        return Ok(new
        {
            success = true,
            message = "Test Tracking Controller is working",
            timestamp = DateTime.UtcNow,
            service = "AfterShip Integration"
        });
    }
}


