using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HubApi.Data;
using HubApi.Models;
using HubApi.Services;

namespace HubApi.Pages.Admin;

[Authorize]
public class SitesModel : PageModel
{
    private readonly OrderHubDbContext _context;
    private readonly ICryptoService _cryptoService;

    public SitesModel(OrderHubDbContext context, ICryptoService cryptoService)
    {
        _context = context;
        _cryptoService = cryptoService;
    }

    public List<Site> Sites { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public string SiteName { get; set; } = string.Empty;

    [BindProperty]
    public string BaseUrl { get; set; } = string.Empty;

    [BindProperty]
    public Guid? DeleteSiteId { get; set; }

    [BindProperty]
    public Guid? ToggleSiteId { get; set; }

    [BindProperty]
    public bool? CurrentStatus { get; set; }

    public async Task OnGetAsync()
    {
        Sites = await _context.Sites
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Handle site deletion
        if (DeleteSiteId.HasValue)
        {
            return await HandleDeleteSite(DeleteSiteId.Value);
        }

        // Handle site status toggle
        if (ToggleSiteId.HasValue && CurrentStatus.HasValue)
        {
            return await HandleToggleSiteStatus(ToggleSiteId.Value, CurrentStatus.Value);
        }

        // Handle site creation
        if (string.IsNullOrEmpty(SiteName) || string.IsNullOrEmpty(BaseUrl))
        {
            ErrorMessage = "Please provide both site name and base URL.";
            return Page();
        }

        try
        {
            // Check if site with same base URL already exists
            var existingSite = await _context.Sites
                .FirstOrDefaultAsync(s => s.BaseUrl == BaseUrl);

            if (existingSite != null)
            {
                ErrorMessage = "A site with this base URL already exists.";
                return Page();
            }

            // Generate API key and secret
            var apiKey = Guid.NewGuid().ToString("N"); // No dashes
            var apiSecret = GenerateRandomSecret(32);

            // Hash the secret for storage
            var secretHash = BCrypt.Net.BCrypt.HashPassword(apiSecret);

            // Encrypt the secret
            var encryptedSecret = _cryptoService.Encrypt(apiSecret);

            var site = new Site
            {
                Id = Guid.NewGuid(),
                Name = SiteName,
                BaseUrl = BaseUrl,
                ApiKey = apiKey,
                ApiSecretHash = secretHash,
                ApiSecretEnc = encryptedSecret,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Sites.Add(site);
            await _context.SaveChangesAsync();

            SuccessMessage = $"Site '{SiteName}' created successfully! API Key: {apiKey}";
            
            // Clear form
            SiteName = string.Empty;
            BaseUrl = string.Empty;

            // Refresh sites list
            await OnGetAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error creating site: {ex.Message}";
        }

        return Page();
    }

    private async Task<IActionResult> HandleDeleteSite(Guid siteId)
    {
        try
        {
            var site = await _context.Sites.FindAsync(siteId);
            if (site == null)
            {
                ErrorMessage = "Site not found.";
                return Page();
            }

            // Delete associated orders and data
            var orders = await _context.Orders.Where(o => o.SiteId == siteId).ToListAsync();
            _context.Orders.RemoveRange(orders);

            // Delete the site
            _context.Sites.Remove(site);
            await _context.SaveChangesAsync();

            SuccessMessage = $"Site '{site.Name}' has been deleted successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error deleting site: {ex.Message}";
        }

        await OnGetAsync();
        return Page();
    }

    private async Task<IActionResult> HandleToggleSiteStatus(Guid siteId, bool currentStatus)
    {
        try
        {
            var site = await _context.Sites.FindAsync(siteId);
            if (site == null)
            {
                ErrorMessage = "Site not found.";
                return Page();
            }

            site.IsActive = !currentStatus;
            site.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var action = currentStatus ? "paused" : "activated";
            SuccessMessage = $"Site '{site.Name}' has been {action} successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error updating site status: {ex.Message}";
        }

        await OnGetAsync();
        return Page();
    }

    private string GenerateRandomSecret(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
