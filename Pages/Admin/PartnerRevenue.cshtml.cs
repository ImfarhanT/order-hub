using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HubApi.Data;
using HubApi.Models;

namespace HubApi.Pages.Admin;

[Authorize]
public class PartnerRevenueModel : PageModel
{
    private readonly OrderHubDbContext _context;

    public PartnerRevenueModel(OrderHubDbContext context)
    {
        _context = context;
    }

    public List<Partner> Partners { get; set; } = new();

    public async Task OnGetAsync()
    {
        Partners = await _context.Partners
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }
}

