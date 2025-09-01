using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HubApi.Data;
using HubApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HubApi.Pages.Admin;

[Authorize]
public class SitePartnersModel : PageModel
{
    private readonly OrderHubDbContext _context;

    public SitePartnersModel(OrderHubDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public List<SitePartnerViewModel> SitePartners { get; set; } = new List<SitePartnerViewModel>();

    [BindProperty]
    public List<Site> Sites { get; set; } = new List<Site>();

    [BindProperty]
    public List<Partner> Partners { get; set; } = new List<Partner>();

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            // Load sites and partners for the form dropdowns
            Sites = await _context.Sites
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();

            Partners = await _context.Partners
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();

            // Load site-partner assignments
            var assignments = await _context.SitePartners
                .Include(sp => sp.Site)
                .Include(sp => sp.Partner)
                .Where(sp => sp.IsActive)
                .OrderBy(sp => sp.Site.Name)
                .ThenBy(sp => sp.Partner.Name)
                .ToListAsync();

            SitePartners = assignments.Select(sp => new SitePartnerViewModel
            {
                Id = sp.Id,
                SiteId = sp.SiteId,
                SiteName = sp.Site.Name,
                PartnerId = sp.PartnerId,
                PartnerName = sp.Partner.Name,
                ShareType = sp.ShareType,
                SharePercentage = sp.SharePercentage,
                IsActive = sp.IsActive,
                Notes = sp.Notes,
                CreatedAt = sp.CreatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            // Log the error and set empty lists
            SitePartners = new List<SitePartnerViewModel>();
            Sites = new List<Site>();
            Partners = new List<Partner>();
            // You can add logging here if needed
        }
        
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // This will be handled by the API controller
        return RedirectToPage();
    }
}

public class SitePartnerViewModel
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; }
    public string SiteName { get; set; } = string.Empty;
    public Guid PartnerId { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public string ShareType { get; set; } = string.Empty;
    public decimal SharePercentage { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
