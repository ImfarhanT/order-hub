using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace HubApi.Pages.Admin;

public class LoginModel : PageModel
{
    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
        // Clear any existing error messages
        ErrorMessage = null;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
        {
            ErrorMessage = "Please enter both email and password.";
            return Page();
        }

        // Get admin credentials from configuration
        var adminEmail = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["ADMIN_EMAIL"];
        var adminPassword = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["ADMIN_PASSWORD"];

        if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
        {
            ErrorMessage = "Admin credentials not configured.";
            return Page();
        }

        // Simple authentication (in production, you'd want to hash passwords)
        if (Email == adminEmail && Password == adminPassword)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, Email),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            await HttpContext.SignInAsync("Cookies", claimsPrincipal);

            return RedirectToPage("/Admin/Dashboard");
        }

        ErrorMessage = "Invalid email or password.";
        return Page();
    }
}
