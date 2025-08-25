using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HubApi.Data;
using HubApi.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HubApi.Pages.Admin;

[Authorize]
public class PartnersModel : PageModel
{
    private readonly OrderHubDbContext _context;

    public PartnersModel(OrderHubDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public List<Partner> Partners { get; set; } = new List<Partner>();

    public async Task<IActionResult> OnGetAsync()
    {
        Partners = await _context.Partners
            .OrderBy(p => p.Name)
            .ToListAsync();
        
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
