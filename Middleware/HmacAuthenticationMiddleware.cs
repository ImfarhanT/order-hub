using System.Text.Json;
using HubApi.Data;
using HubApi.Services;
using Microsoft.EntityFrameworkCore;

namespace HubApi.Middleware;

public class HmacAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HmacAuthenticationMiddleware> _logger;

    public HmacAuthenticationMiddleware(RequestDelegate next, ILogger<HmacAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, OrderHubDbContext dbContext, IHmacService hmacService, ICryptoService cryptoService)
    {
        // Only apply to plugin endpoints
        if (!context.Request.Path.StartsWithSegments("/api/v1/orders") && 
            !context.Request.Path.StartsWithSegments("/api/v1/shipping"))
        {
            await _next(context);
            return;
        }

        try
        {
            // Read the request body
            context.Request.EnableBuffering();
            var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
            context.Request.Body.Position = 0;

            // Parse the request to get authentication parameters
            var jsonDocument = JsonDocument.Parse(requestBody);
            var root = jsonDocument.RootElement;

            var siteApiKey = root.GetProperty("site_api_key").GetString();
            var nonce = root.GetProperty("nonce").GetString();
            var timestamp = root.GetProperty("timestamp").GetInt64();
            var signature = root.GetProperty("signature").GetString();

            if (string.IsNullOrEmpty(siteApiKey) || string.IsNullOrEmpty(nonce) || 
                string.IsNullOrEmpty(signature) || timestamp == 0)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new { error = "Missing authentication parameters" });
                return;
            }

            // Validate timestamp (within 10 minutes)
            var requestTime = DateTimeOffset.FromUnixTimeSeconds(timestamp);
            var now = DateTimeOffset.UtcNow;
            if (Math.Abs((now - requestTime).TotalMinutes) > 10)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { error = "Request timestamp too old or too new" });
                return;
            }

            // Get site and validate nonce
            var site = await dbContext.Sites
                .FirstOrDefaultAsync(s => s.ApiKey == siteApiKey && s.IsActive);

            if (site == null)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid API key" });
                return;
            }

            // Check if nonce has been used
            var existingNonce = await dbContext.RequestNonces
                .FirstOrDefaultAsync(rn => rn.SiteId == site.Id && rn.Nonce == nonce);

            if (existingNonce != null)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { error = "Nonce already used" });
                return;
            }

            // Store nonce to prevent replay
            var requestNonce = new Models.RequestNonce
            {
                Id = Guid.NewGuid(),
                SiteId = site.Id,
                Nonce = nonce,
                Timestamp = requestTime.DateTime,
                Expires = now.DateTime.AddMinutes(15)
            };

            dbContext.RequestNonces.Add(requestNonce);
            await dbContext.SaveChangesAsync();

            // Decrypt API secret
            var apiSecret = cryptoService.Decrypt(site.ApiSecretEnc);

            // Build signature base
            string signatureBase;
            if (context.Request.Path.StartsWithSegments("/api/v1/orders"))
            {
                var orderId = root.GetProperty("order").GetProperty("wc_order_id").GetString();
                var orderTotal = root.GetProperty("order").GetProperty("order_total").GetDecimal();
                signatureBase = $"{siteApiKey}|{timestamp}|{nonce}|{orderId}|{orderTotal}";
            }
            else if (context.Request.Path.StartsWithSegments("/api/v1/shipping"))
            {
                var orderId = root.GetProperty("wc_order_id").GetString();
                var orderTotal = 0m; // Shipping updates don't have order total in signature
                signatureBase = $"{siteApiKey}|{timestamp}|{nonce}|{orderId}|{orderTotal}";
            }
            else
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new { error = "Unsupported endpoint" });
                return;
            }

            // Verify signature
            if (!hmacService.VerifySignature(signatureBase, signature, apiSecret))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid signature" });
                return;
            }

            // Add site to context for controllers to use
            context.Items["Site"] = site;

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in HMAC authentication middleware");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new { error = "Internal server error" });
        }
    }
}
