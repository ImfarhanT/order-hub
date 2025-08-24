using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HubApi.Pages.Admin;

[Authorize]
public class PartnersModel : PageModel
{
    public void OnGet()
    {
        // Partners functionality coming soon
    }
}
