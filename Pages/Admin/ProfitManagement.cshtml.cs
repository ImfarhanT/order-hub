using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HubApi.Pages.Admin;

[Authorize]
public class ProfitManagementModel : PageModel
{
    public void OnGet()
    {
        // Page model - no additional logic needed for now
    }
}

