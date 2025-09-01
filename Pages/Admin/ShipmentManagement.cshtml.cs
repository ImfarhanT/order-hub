using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HubApi.Pages.Admin;

[Authorize]
public class ShipmentManagementModel : PageModel
{
    public void OnGet()
    {
        // Page initialization logic will be handled by JavaScript
    }
}

