using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HubApi.Data;
using HubApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HubApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PartnersController : ControllerBase
{
    private readonly OrderHubDbContext _context;

    public PartnersController(OrderHubDbContext context)
    {
        _context = context;
    }

    // GET: api/partners
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Partner>>> GetPartners()
    {
        return await _context.Partners
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    // GET: api/partners/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Partner>> GetPartner(Guid id)
    {
        var partner = await _context.Partners.FindAsync(id);

        if (partner == null)
        {
            return NotFound();
        }

        return partner;
    }

    // POST: api/partners
    [HttpPost]
    public async Task<ActionResult<Partner>> CreatePartner(Partner partner)
    {
        if (ModelState.IsValid)
        {
            partner.Id = Guid.NewGuid();
            partner.CreatedAt = DateTime.UtcNow;
            partner.UpdatedAt = DateTime.UtcNow;

            _context.Partners.Add(partner);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPartner), new { id = partner.Id }, partner);
        }

        return BadRequest(ModelState);
    }

    // PUT: api/partners/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePartner(Guid id, Partner partner)
    {
        if (id != partner.Id)
        {
            return BadRequest();
        }

        if (ModelState.IsValid)
        {
            var existingPartner = await _context.Partners.FindAsync(id);
            if (existingPartner == null)
            {
                return NotFound();
            }

            // Update properties
            existingPartner.Name = partner.Name;
            existingPartner.Email = partner.Email;
            existingPartner.Phone = partner.Phone;
            existingPartner.Company = partner.Company;
            existingPartner.ShareType = partner.ShareType;
            existingPartner.SharePercentage = partner.SharePercentage;
            existingPartner.Address = partner.Address;
            existingPartner.Notes = partner.Notes;
            existingPartner.IsActive = partner.IsActive;
            existingPartner.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PartnerExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        return BadRequest(ModelState);
    }

    // DELETE: api/partners/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePartner(Guid id)
    {
        var partner = await _context.Partners.FindAsync(id);
        if (partner == null)
        {
            return NotFound();
        }

        // Check if partner has any orders
        var hasOrders = await _context.PartnerOrders.AnyAsync(po => po.PartnerId == id);
        if (hasOrders)
        {
            return BadRequest("Cannot delete partner with existing orders. Deactivate instead.");
        }

        _context.Partners.Remove(partner);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // GET: api/partners/5/earnings
    [HttpGet("{id}/earnings")]
    public async Task<ActionResult<object>> GetPartnerEarnings(Guid id)
    {
        var partner = await _context.Partners.FindAsync(id);
        if (partner == null)
        {
            return NotFound();
        }

        var earnings = await _context.PartnerOrders
            .Where(po => po.PartnerId == id)
            .GroupBy(po => new { po.ShareType, po.IsPaid })
            .Select(g => new
            {
                ShareType = g.Key.ShareType,
                IsPaid = g.Key.IsPaid,
                TotalAmount = g.Sum(po => po.ShareAmount),
                OrderCount = g.Count()
            })
            .ToListAsync();

        var totalEarnings = await _context.PartnerOrders
            .Where(po => po.PartnerId == id)
            .SumAsync(po => po.ShareAmount);

        var paidEarnings = await _context.PartnerOrders
            .Where(po => po.PartnerId == id && po.IsPaid)
            .SumAsync(po => po.ShareAmount);

        var unpaidEarnings = totalEarnings - paidEarnings;

        return new
        {
            Partner = new
            {
                partner.Id,
                partner.Name,
                partner.ShareType,
                partner.SharePercentage
            },
            Earnings = earnings,
            Summary = new
            {
                TotalEarnings = totalEarnings,
                PaidEarnings = paidEarnings,
                UnpaidEarnings = unpaidEarnings
            }
        };
    }

    private bool PartnerExists(Guid id)
    {
        return _context.Partners.Any(e => e.Id == id);
    }
}
