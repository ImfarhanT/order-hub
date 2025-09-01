using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HubApi.Data;
using HubApi.Models;

namespace HubApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class PaymentGatewayDetailsController : ControllerBase
{
    private readonly OrderHubDbContext _context;
    private readonly ILogger<PaymentGatewayDetailsController> _logger;

    public PaymentGatewayDetailsController(OrderHubDbContext context, ILogger<PaymentGatewayDetailsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all payment gateway details
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetPaymentGatewayDetails()
    {
        try
        {
            var gateways = await _context.PaymentGatewayDetails
                .Select(g => new
                {
                    g.Id,
                    g.GatewayCode,
                    g.Descriptor,
                    g.FeesPercentage,
                    g.FeesFixed,
                    g.FeeType
                })
                .OrderBy(g => g.GatewayCode)
                .ToListAsync();

            return Ok(gateways);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment gateway details");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific payment gateway detail by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetPaymentGatewayDetail(Guid id)
    {
        try
        {
            var gateway = await _context.PaymentGatewayDetails
                .Where(g => g.Id == id)
                .Select(g => new
                {
                    g.Id,
                    g.GatewayCode,
                    g.Descriptor,
                    g.FeesPercentage,
                    g.FeesFixed,
                    g.FeeType
                })
                .FirstOrDefaultAsync();

            if (gateway == null)
            {
                return NotFound(new { error = "Payment gateway not found" });
            }

            return Ok(gateway);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment gateway detail {Id}", id);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Create a new payment gateway detail
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<object>> CreatePaymentGatewayDetail([FromBody] PaymentGatewayDetailsRequest request)
    {
        try
        {
            // Check if gateway code already exists
            var existing = await _context.PaymentGatewayDetails
                .FirstOrDefaultAsync(g => g.GatewayCode == request.GatewayCode);

            if (existing != null)
            {
                return BadRequest(new { error = "Gateway code already exists" });
            }

            var gateway = new PaymentGatewayDetails
            {
                Id = Guid.NewGuid(),
                GatewayCode = request.GatewayCode,
                Descriptor = request.Descriptor,
                FeesPercentage = request.FeeType == "percentage" ? request.FeesValue : null,
                FeesFixed = request.FeeType == "fixed" ? request.FeesValue : null,
                FeeType = request.FeeType
            };

            _context.PaymentGatewayDetails.Add(gateway);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created payment gateway detail: {GatewayCode}", gateway.GatewayCode);

            return CreatedAtAction(nameof(GetPaymentGatewayDetail), new { id = gateway.Id }, new
            {
                success = true,
                id = gateway.Id,
                gatewayCode = gateway.GatewayCode,
                descriptor = gateway.Descriptor,
                feesPercentage = gateway.FeesPercentage,
                feesFixed = gateway.FeesFixed,
                feeType = gateway.FeeType
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment gateway detail");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing payment gateway detail
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePaymentGatewayDetail(Guid id, [FromBody] PaymentGatewayDetailsRequest request)
    {
        try
        {
            var existing = await _context.PaymentGatewayDetails.FindAsync(id);
            if (existing == null)
            {
                return NotFound(new { error = "Payment gateway not found" });
            }

            // Check if gateway code already exists for a different record
            var duplicateCode = await _context.PaymentGatewayDetails
                .FirstOrDefaultAsync(g => g.GatewayCode == request.GatewayCode && g.Id != id);

            if (duplicateCode != null)
            {
                return BadRequest(new { error = "Gateway code already exists" });
            }

            existing.GatewayCode = request.GatewayCode;
            existing.Descriptor = request.Descriptor;
            existing.FeesPercentage = request.FeeType == "percentage" ? request.FeesValue : null;
            existing.FeesFixed = request.FeeType == "fixed" ? request.FeesValue : null;
            existing.FeeType = request.FeeType;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated payment gateway detail: {GatewayCode}", existing.GatewayCode);

            return Ok(new
            {
                success = true,
                id = existing.Id,
                gatewayCode = existing.GatewayCode,
                descriptor = existing.Descriptor,
                feesPercentage = existing.FeesPercentage,
                feesFixed = existing.FeesFixed,
                feeType = existing.FeeType
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment gateway detail {Id}", id);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Delete a payment gateway detail
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePaymentGatewayDetail(Guid id)
    {
        try
        {
            var gateway = await _context.PaymentGatewayDetails.FindAsync(id);
            if (gateway == null)
            {
                return NotFound(new { error = "Payment gateway not found" });
            }

            _context.PaymentGatewayDetails.Remove(gateway);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted payment gateway detail: {GatewayCode}", gateway.GatewayCode);

            return Ok(new { success = true, message = "Payment gateway deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payment gateway detail {Id}", id);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }
}

public class PaymentGatewayDetailsRequest
{
    public string GatewayCode { get; set; } = string.Empty;
    public string? Descriptor { get; set; }
    public decimal FeesValue { get; set; }
    public string FeeType { get; set; } = "percentage"; // "percentage" or "fixed"
}

