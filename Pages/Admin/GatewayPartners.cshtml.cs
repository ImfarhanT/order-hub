using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HubApi.Models;

namespace HubApi.Pages.Admin
{
    public class GatewayPartnersModel : PageModel
    {
        public List<GatewayPartner> GatewayPartners { get; set; } = new();
        public List<GatewayPartnerAssignment> Assignments { get; set; } = new();
        public List<PaymentGatewayDetails> PaymentGateways { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            // This page will load data via JavaScript API calls
            return Page();
        }
    }
}
