using Microsoft.AspNetCore.Mvc;
using HubApi.Services;
using HubApi.DTOs;

namespace HubApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class GatewayPartnerRevenueController : ControllerBase
{
    private readonly IGatewayPartnerRevenueService _revenueService;
    private readonly IPdfReportService _pdfService;
    private readonly ILogger<GatewayPartnerRevenueController> _logger;

    public GatewayPartnerRevenueController(
        IGatewayPartnerRevenueService revenueService, 
        IPdfReportService pdfService,
        ILogger<GatewayPartnerRevenueController> logger)
    {
        _revenueService = revenueService;
        _pdfService = pdfService;
        _logger = logger;
    }

    [HttpGet("report")]
    public async Task<IActionResult> GetRevenueReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        try
        {
            if (startDate >= endDate)
            {
                return BadRequest(new { error = "Start date must be before end date" });
            }

            var report = await _revenueService.CalculateRevenueAsync(startDate, endDate);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating revenue report for period {StartDate} to {EndDate}", startDate, endDate);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [HttpGet("gateway/{gatewayId}/report")]
    public async Task<IActionResult> GetGatewayRevenueReport(Guid gatewayId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        try
        {
            if (startDate >= endDate)
            {
                return BadRequest(new { error = "Start date must be before end date" });
            }

            var report = await _revenueService.CalculateRevenueForGatewayAsync(gatewayId, startDate, endDate);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating revenue report for gateway {GatewayId}", gatewayId);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [HttpGet("partner/{partnerId}/report")]
    public async Task<IActionResult> GetPartnerRevenueReport(Guid partnerId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        try
        {
            if (startDate >= endDate)
            {
                return BadRequest(new { error = "Start date must be before end date" });
            }

            var report = await _revenueService.CalculateRevenueForPartnerAsync(partnerId, startDate, endDate);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating revenue report for partner {PartnerId}", partnerId);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetRevenueSummary([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        try
        {
            if (startDate >= endDate)
            {
                return BadRequest(new { error = "Start date must be before end date" });
            }

            var report = await _revenueService.CalculateRevenueAsync(startDate, endDate);
            
            var summary = new
            {
                period = new { startDate, endDate },
                totalRevenue = report.TotalRevenue,
                totalOrders = report.OrderDetails.Count,
                gatewayCount = report.GatewaySummaries.Count,
                partnerCount = report.PartnerSummaries.Count,
                topGateways = report.GatewaySummaries.OrderByDescending(g => g.TotalRevenue).Take(5),
                topPartners = report.PartnerSummaries.OrderByDescending(p => p.TotalRevenueShare).Take(5)
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating revenue summary for period {StartDate} to {EndDate}", startDate, endDate);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [HttpGet("gateway/{gatewayId}/pdf")]
    public async Task<IActionResult> ExportGatewayPdf(Guid gatewayId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        try
        {
            if (startDate >= endDate)
            {
                return BadRequest(new { error = "Start date must be before end date" });
            }

            var report = await _revenueService.CalculateRevenueForGatewayAsync(gatewayId, startDate, endDate);
            
            if (report.GatewaySummaries.Count == 0)
            {
                return NotFound(new { error = "Gateway not found or no revenue data" });
            }

            var gateway = report.GatewaySummaries.First();
            var pdfBytes = _pdfService.GenerateGatewayRevenuePdf(gateway, report.OrderDetails, startDate, endDate);

            var fileName = $"gateway-{gateway.GatewayCode}-revenue-{startDate:yyyy-MM-dd}-{endDate:yyyy-MM-dd}.pdf";
            
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for gateway {GatewayId}", gatewayId);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [HttpGet("partner/{partnerId}/pdf")]
    public async Task<IActionResult> ExportPartnerPdf(Guid partnerId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        try
        {
            if (startDate >= endDate)
            {
                return BadRequest(new { error = "Start date must be before end date" });
            }

            var report = await _revenueService.CalculateRevenueForPartnerAsync(partnerId, startDate, endDate);
            
            if (report.PartnerSummaries.Count == 0)
            {
                return NotFound(new { error = "Partner not found or no revenue data" });
            }

            var partner = report.PartnerSummaries.First();
            var pdfBytes = _pdfService.GeneratePartnerRevenuePdf(partner, report.OrderDetails, startDate, endDate);

            var fileName = $"partner-{partner.PartnerCode}-revenue-{startDate:yyyy-MM-dd}-{endDate:yyyy-MM-dd}.pdf";
            
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for partner {PartnerId}", partnerId);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [HttpGet("full-pdf")]
    public async Task<IActionResult> ExportFullPdf([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        try
        {
            if (startDate >= endDate)
            {
                return BadRequest(new { error = "Start date must be before end date" });
            }

            var report = await _revenueService.CalculateRevenueAsync(startDate, endDate);
            var pdfBytes = _pdfService.GenerateFullRevenuePdf(report);

            var fileName = $"full-revenue-report-{startDate:yyyy-MM-dd}-{endDate:yyyy-MM-dd}.pdf";
            
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating full PDF report for period {StartDate} to {EndDate}", startDate, endDate);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }
}
