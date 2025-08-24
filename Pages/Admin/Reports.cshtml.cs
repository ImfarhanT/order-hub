using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HubApi.Pages.Admin;

[Authorize]
public class ReportsModel : PageModel
{
    public void OnGet()
    {
        // Reports functionality coming soon
    }
}
