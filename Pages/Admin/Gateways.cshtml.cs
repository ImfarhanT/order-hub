using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HubApi.Pages.Admin;

[Authorize]
public class GatewaysModel : PageModel
{
    public void OnGet()
    {
        // Gateways functionality coming soon
    }
}
