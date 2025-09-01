using System.Text.Json;
using HubApi.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HubApi.Services;

public class SeventeenTrackService : ITrackingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SeventeenTrackService> _logger;
    private readonly string _apiKey;
    private readonly string _baseUrl = "https://api.17track.net/v2";

    public SeventeenTrackService(
        HttpClient httpClient, 
        IConfiguration configuration,
        ILogger<SeventeenTrackService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["Tracking:17Track:ApiKey"] ?? string.Empty;
        
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("17track API key not configured. Live tracking will not work.");
        }
    }

    public async Task<TrackingResponse> GetLiveTrackingAsync(string trackingNumber, string carrier)
    {
        try
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                return new TrackingResponse
                {
                    TrackingNumber = trackingNumber,
                    Carrier = carrier,
                    Status = "API not configured",
                    HasException = true,
                    ExceptionMessage = "17track API key not configured"
                };
            }

            _logger.LogInformation("Getting live tracking for {TrackingNumber} via {Carrier}", trackingNumber, carrier);

            var requestData = new
            {
                method = "track",
                data = new
                {
                    number = trackingNumber,
                    carrier = carrier
                }
            };

            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            // Add API key to headers
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("17token", _apiKey);

            var response = await _httpClient.PostAsync($"{_baseUrl}/track", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("17track API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return new TrackingResponse
                {
                    TrackingNumber = trackingNumber,
                    Carrier = carrier,
                    Status = "API Error",
                    HasException = true,
                    ExceptionMessage = $"API Error: {response.StatusCode}"
                };
            }

            var trackingData = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            // Parse 17track response format
            return Parse17TrackResponse(trackingNumber, carrier, trackingData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting live tracking for {TrackingNumber}", trackingNumber);
            return new TrackingResponse
            {
                TrackingNumber = trackingNumber,
                Carrier = carrier,
                Status = "Error",
                HasException = true,
                ExceptionMessage = ex.Message
            };
        }
    }

    public async Task<List<TrackingResponse>> GetBulkTrackingAsync(List<string> trackingNumbers)
    {
        var results = new List<TrackingResponse>();
        
        foreach (var trackingNumber in trackingNumbers)
        {
            var result = await GetLiveTrackingAsync(trackingNumber, "");
            results.Add(result);
            
            // Rate limiting - 17track allows 100 requests per minute
            await Task.Delay(600); // 600ms between requests = 100 per minute
        }
        
        return results;
    }

    public async Task<bool> IsTrackingNumberValidAsync(string trackingNumber)
    {
        try
        {
            var tracking = await GetLiveTrackingAsync(trackingNumber, "");
            return !tracking.HasException && !string.IsNullOrEmpty(tracking.Status);
        }
        catch
        {
            return false;
        }
    }

    private TrackingResponse Parse17TrackResponse(string trackingNumber, string carrier, JsonElement data)
    {
        try
        {
            var response = new TrackingResponse
            {
                TrackingNumber = trackingNumber,
                Carrier = carrier,
                LastUpdated = DateTime.UtcNow
            };

            // Parse the 17track response structure
            if (data.TryGetProperty("data", out var dataElement) && 
                dataElement.TryGetProperty("accepted", out var acceptedElement))
            {
                var accepted = acceptedElement.EnumerateArray().FirstOrDefault();
                if (accepted.ValueKind != JsonValueKind.Undefined)
                {
                    // Parse tracking details
                    if (accepted.TryGetProperty("tracking_number", out var tn))
                        response.TrackingNumber = tn.GetString() ?? trackingNumber;
                    
                    if (accepted.TryGetProperty("carrier_code", out var cc))
                        response.Carrier = cc.GetString() ?? carrier;
                    
                    if (accepted.TryGetProperty("status", out var status))
                        response.Status = status.GetString() ?? "Unknown";
                    
                    if (accepted.TryGetProperty("location", out var location))
                        response.CurrentLocation = location.GetString() ?? "";
                    
                    if (accepted.TryGetProperty("estimated_delivery", out var ed))
                    {
                        if (DateTime.TryParse(ed.GetString(), out var estimatedDate))
                            response.EstimatedDelivery = estimatedDate;
                    }
                    
                    if (accepted.TryGetProperty("delivered_time", out var dt))
                    {
                        if (DateTime.TryParse(dt.GetString(), out var deliveredDate))
                        {
                            response.DeliveredAt = deliveredDate;
                            response.IsDelivered = true;
                        }
                    }
                    
                    // Parse tracking events
                    if (accepted.TryGetProperty("tracking_detail", out var details))
                    {
                        foreach (var detail in details.EnumerateArray())
                        {
                            var trackingEvent = new TrackingEvent();
                            
                            if (detail.TryGetProperty("time", out var time))
                            {
                                if (DateTime.TryParse(time.GetString(), out var eventTime))
                                    trackingEvent.Timestamp = eventTime;
                            }
                            
                            if (detail.TryGetProperty("location", out var eventLocation))
                                trackingEvent.Location = eventLocation.GetString() ?? "";
                            
                            if (detail.TryGetProperty("status", out var eventStatus))
                                trackingEvent.Status = eventStatus.GetString() ?? "";
                            
                            if (detail.TryGetProperty("description", out var description))
                                trackingEvent.Description = description.GetString() ?? "";
                            
                            response.Events.Add(trackingEvent);
                        }
                    }
                    
                    // Check for exceptions
                    if (accepted.TryGetProperty("exception", out var exception))
                    {
                        response.HasException = true;
                        response.ExceptionMessage = exception.GetString() ?? "Unknown exception";
                    }
                }
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing 17track response for {TrackingNumber}", trackingNumber);
            return new TrackingResponse
            {
                TrackingNumber = trackingNumber,
                Carrier = carrier,
                Status = "Parse Error",
                HasException = true,
                ExceptionMessage = $"Parse Error: {ex.Message}"
            };
        }
    }
}


