using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HubApi.Data;
using HubApi.Models;
using HubApi.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HubApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class SitePartnersController : ControllerBase
{
    private readonly OrderHubDbContext _context;
    private readonly ILogger<SitePartnersController> _logger;

    public SitePartnersController(OrderHubDbContext context, ILogger<SitePartnersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/sitepartners
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetSitePartners()
    {
        var sitePartners = await _context.SitePartners
            .Include(sp => sp.Site)
            .Include(sp => sp.Partner)
            .Where(sp => sp.IsActive)
            .OrderBy(sp => sp.Site.Name)
            .ThenBy(sp => sp.Partner.Name)
            .Select(sp => new
            {
                sp.Id,
                SiteId = sp.Site.Id,
                SiteName = sp.Site.Name,
                PartnerId = sp.Partner.Id,
                PartnerName = sp.Partner.Name,
                sp.ShareType,
                sp.SharePercentage,
                sp.IsActive,
                sp.Notes,
                sp.CreatedAt
            })
            .ToListAsync();

        return Ok(sitePartners);
    }

    // GET: api/sitepartners/site/{siteId}
    [HttpGet("site/{siteId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetSitePartnersBySite(Guid siteId)
    {
        var sitePartners = await _context.SitePartners
            .Include(sp => sp.Partner)
            .Where(sp => sp.SiteId == siteId && sp.IsActive)
            .OrderBy(sp => sp.Partner.Name)
            .Select(sp => new
            {
                sp.Id,
                PartnerId = sp.Partner.Id,
                PartnerName = sp.Partner.Name,
                sp.ShareType,
                sp.SharePercentage,
                sp.IsActive,
                sp.Notes
            })
            .ToListAsync();

        return Ok(sitePartners);
    }

    // GET: api/sitepartners/partner/{partnerId}
    [HttpGet("partner/{partnerId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetSitePartnersByPartner(Guid partnerId)
    {
        var sitePartners = await _context.SitePartners
            .Include(sp => sp.Site)
            .Where(sp => sp.PartnerId == partnerId && sp.IsActive)
            .OrderBy(sp => sp.Site.Name)
            .Select(sp => new
            {
                sp.Id,
                SiteId = sp.Site.Id,
                SiteName = sp.Site.Name,
                sp.ShareType,
                sp.SharePercentage,
                sp.IsActive,
                sp.Notes
            })
            .ToListAsync();

        return Ok(sitePartners);
    }

    // GET: api/sitepartners/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetSitePartner(Guid id)
    {
        var sitePartner = await _context.SitePartners
            .Include(sp => sp.Site)
            .Include(sp => sp.Partner)
            .FirstOrDefaultAsync(sp => sp.Id == id);

        if (sitePartner == null)
        {
            return NotFound();
        }

        return Ok(new
        {
            id = sitePartner.Id,
            siteId = sitePartner.SiteId,
            siteName = sitePartner.Site.Name,
            partnerId = sitePartner.PartnerId,
            partnerName = sitePartner.Partner.Name,
            shareType = sitePartner.ShareType,
            sharePercentage = sitePartner.SharePercentage,
            isActive = sitePartner.IsActive,
            notes = sitePartner.Notes,
            createdAt = sitePartner.CreatedAt
        });
    }

    // POST: api/sitepartners
    [HttpPost]
    public async Task<ActionResult<SitePartner>> CreateSitePartner([FromBody] SitePartnerRequest request)
    {
        _logger.LogInformation("Creating site-partner assignment: Site {SiteId}, Partner {PartnerId}", 
            request.siteId, request.partnerId);

        // Check if assignment already exists
        var existing = await _context.SitePartners
            .FirstOrDefaultAsync(sp => sp.SiteId == request.siteId && sp.PartnerId == request.partnerId);

        if (existing != null)
        {
            return BadRequest("This site-partner assignment already exists");
        }

        // Validate site exists
        var site = await _context.Sites.FindAsync(request.siteId);
        if (site == null)
        {
            return BadRequest("Site not found");
        }

        // Validate partner exists
        var partner = await _context.Partners.FindAsync(request.partnerId);
        if (partner == null)
        {
            return BadRequest("Partner not found");
        }

        var sitePartner = new SitePartner
        {
            Id = Guid.NewGuid(),
            SiteId = request.siteId,
            PartnerId = request.partnerId,
            ShareType = request.shareType,
            SharePercentage = request.sharePercentage,
            IsActive = request.isActive,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.SitePartners.Add(sitePartner);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Site-partner assignment created successfully with ID: {Id}", sitePartner.Id);
        
        // Return a clean response object to avoid circular references
        var response = new
        {
            id = sitePartner.Id,
            siteId = sitePartner.SiteId,
            partnerId = sitePartner.PartnerId,
            shareType = sitePartner.ShareType,
            sharePercentage = sitePartner.SharePercentage,
            isActive = sitePartner.IsActive,
            notes = sitePartner.Notes,
            createdAt = sitePartner.CreatedAt
        };
        
        return CreatedAtAction(nameof(GetSitePartners), new { id = sitePartner.Id }, response);
    }

    // PUT: api/sitepartners/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSitePartner(Guid id, [FromBody] SitePartnerRequest request)
    {
        var existing = await _context.SitePartners.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        // Update properties
        existing.ShareType = request.shareType;
        existing.SharePercentage = request.sharePercentage;
        existing.IsActive = request.isActive;
        existing.Notes = request.Notes;
        existing.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Site-partner assignment updated successfully: {Id}", id);
            return NoContent();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!SitePartnerExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }
    }

    // DELETE: api/sitepartners/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSitePartner(Guid id)
    {
        var sitePartner = await _context.SitePartners.FindAsync(id);
        if (sitePartner == null)
        {
            return NotFound();
        }

        // Check if there are any orders with this assignment
        var hasOrders = await _context.PartnerOrders
            .AnyAsync(po => po.PartnerId == sitePartner.PartnerId);

        if (hasOrders)
        {
            // Deactivate instead of delete
            sitePartner.IsActive = false;
            sitePartner.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Site-partner assignment deactivated: {Id}", id);
            return NoContent();
        }

        _context.SitePartners.Remove(sitePartner);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Site-partner assignment deleted: {Id}", id);
        return NoContent();
    }

    private bool SitePartnerExists(Guid id)
    {
        return _context.SitePartners.Any(e => e.Id == id);
    }
}
