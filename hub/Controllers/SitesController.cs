using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HubApi.Data;
using HubApi.DTOs;
using HubApi.Models;
using HubApi.Services;
using BCrypt.Net;

namespace HubApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SitesController : ControllerBase
{
    private readonly OrderHubDbContext _context;
    private readonly ICryptoService _cryptoService;
    private readonly ILogger<SitesController> _logger;

    public SitesController(OrderHubDbContext context, ICryptoService cryptoService, ILogger<SitesController> logger)
    {
        _context = context;
        _cryptoService = cryptoService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<SiteResponse>> CreateSite([FromBody] SiteCreateRequest request)
    {
        try
        {
            // Check if site with same base URL already exists
            var existingSite = await _context.Sites
                .FirstOrDefaultAsync(s => s.BaseUrl == request.BaseUrl);

            if (existingSite != null)
                return BadRequest(new { error = "Site with this base URL already exists" });

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
                Name = request.Name,
                BaseUrl = request.BaseUrl,
                ApiKey = apiKey,
                ApiSecretHash = secretHash,
                ApiSecretEnc = encryptedSecret,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Sites.Add(site);
            await _context.SaveChangesAsync();

            var response = new SiteResponse
            {
                Id = site.Id,
                Name = site.Name,
                BaseUrl = site.BaseUrl,
                ApiKey = site.ApiKey,
                ApiSecret = apiSecret, // Show only once
                IsActive = site.IsActive,
                CreatedAt = site.CreatedAt,
                UpdatedAt = site.UpdatedAt
            };

            return CreatedAtAction(nameof(GetSite), new { id = site.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating site {SiteName}", request.Name);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SiteResponse>> GetSite(Guid id)
    {
        var site = await _context.Sites.FindAsync(id);
        if (site == null)
            return NotFound();

        var response = new SiteResponse
        {
            Id = site.Id,
            Name = site.Name,
            BaseUrl = site.BaseUrl,
            ApiKey = site.ApiKey,
            ApiSecret = null, // Never show secret after creation
            IsActive = site.IsActive,
            CreatedAt = site.CreatedAt,
            UpdatedAt = site.UpdatedAt
        };

        return Ok(response);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SiteResponse>>> GetSites()
    {
        var sites = await _context.Sites
            .OrderBy(s => s.Name)
            .ToListAsync();

        var responses = sites.Select(s => new SiteResponse
        {
            Id = s.Id,
            Name = s.Name,
            BaseUrl = s.BaseUrl,
            ApiKey = s.ApiKey,
            ApiSecret = null, // Never show secrets in list
            IsActive = s.IsActive,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        });

        return Ok(responses);
    }

    private static string GenerateRandomSecret(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
