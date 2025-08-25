using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HubApi.Data;
using HubApi.Models;

namespace HubApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RawDataController : ControllerBase
    {
        private readonly OrderHubDbContext _context;
        private readonly ILogger<RawDataController> _logger;

        public RawDataController(OrderHubDbContext context, ILogger<RawDataController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get raw order data by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetRawData(Guid id)
        {
            try
            {
                var rawData = await _context.RawOrderData
                    .Include(r => r.Site)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (rawData == null)
                {
                    return NotFound(new { error = "Raw data not found" });
                }

                // Parse the JSON to return it properly formatted
                var jsonData = System.Text.Json.JsonDocument.Parse(rawData.RawJson);

                return Ok(new
                {
                    id = rawData.Id,
                    siteId = rawData.SiteId,
                    siteName = rawData.SiteName,
                    rawJson = jsonData,
                    receivedAt = rawData.ReceivedAt,
                    processed = rawData.Processed,
                    processedAt = rawData.ProcessedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching raw data {Id}", id);
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all raw order data with pagination
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetRawDataList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] Guid? siteId = null,
            [FromQuery] bool? processed = null)
        {
            try
            {
                pageSize = Math.Max(1, Math.Min(100, pageSize)); // Limit page size between 1 and 100

                var query = _context.RawOrderData
                    .Include(r => r.Site)
                    .AsQueryable();

                // Apply filters
                if (siteId.HasValue)
                {
                    query = query.Where(r => r.SiteId == siteId.Value);
                }

                if (processed.HasValue)
                {
                    query = query.Where(r => r.Processed == processed.Value);
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var rawData = await query
                    .OrderByDescending(r => r.ReceivedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new
                    {
                        id = r.Id,
                        siteId = r.SiteId,
                        siteName = r.SiteName,
                        receivedAt = r.ReceivedAt,
                        processed = r.Processed,
                        processedAt = r.ProcessedAt,
                        jsonPreview = r.RawJson.Length > 100 ? r.RawJson.Substring(0, 100) + "..." : r.RawJson
                    })
                    .ToListAsync();

                return Ok(new
                {
                    data = rawData,
                    pagination = new
                    {
                        page,
                        pageSize,
                        totalCount,
                        totalPages
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching raw data list");
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete raw order data
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRawData(Guid id)
        {
            try
            {
                var rawData = await _context.RawOrderData.FindAsync(id);
                if (rawData == null)
                {
                    return NotFound(new { error = "Raw data not found" });
                }

                _context.RawOrderData.Remove(rawData);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Raw order data deleted: {Id}", id);
                return Ok(new { ok = true, message = "Raw data deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting raw data {Id}", id);
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Mark raw order data as processed
        /// </summary>
        [HttpPut("{id}/processed")]
        public async Task<IActionResult> MarkAsProcessed(Guid id)
        {
            try
            {
                var rawData = await _context.RawOrderData.FindAsync(id);
                if (rawData == null)
                {
                    return NotFound(new { error = "Raw data not found" });
                }

                rawData.Processed = true;
                rawData.ProcessedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Raw order data marked as processed: {Id}", id);
                return Ok(new { ok = true, message = "Raw data marked as processed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking raw data as processed {Id}", id);
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }
    }
}

