using System.Text.Json;
using HubApi.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HubApi.Services;

public class AfterShipTrackingService : ITrackingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AfterShipTrackingService> _logger;
    private readonly string _apiKey;
    private readonly string _baseUrl = "https://api.aftership.com/v4";

    public AfterShipTrackingService(
        HttpClient httpClient, 
        IConfiguration configuration,
        ILogger<AfterShipTrackingService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["Tracking:AfterShip:ApiKey"] ?? string.Empty;
        
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("AfterShip API key not configured. Live tracking will not work.");
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
                    ExceptionMessage = "AfterShip API key not configured"
                };
            }

            _logger.LogInformation("Getting live tracking for {TrackingNumber} via {Carrier}", trackingNumber, carrier);

            // AfterShip API call
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("aftership-api-key", _apiKey);

            // AfterShip API format: /trackings?tracking_number={number}
            var response = await _httpClient.GetAsync($"{_baseUrl}/trackings?tracking_number={trackingNumber}");
            var responseContent = await response.Content.ReadAsStringAsync();
            
            // Debug logging to see the actual API response
            _logger.LogInformation("AfterShip API Response for {TrackingNumber}: {Response}", trackingNumber, responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("AfterShip API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
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
            
            // Parse AfterShip response format
            return ParseAfterShipResponse(trackingNumber, carrier, trackingData);
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
            
            // Rate limiting - AfterShip allows 1,000 requests per month
            await Task.Delay(100); // 100ms between requests
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

    private TrackingResponse ParseAfterShipResponse(string trackingNumber, string carrier, JsonElement data)
    {
        try
        {
            var response = new TrackingResponse
            {
                TrackingNumber = trackingNumber,
                Carrier = carrier,
                LastUpdated = DateTime.UtcNow
            };

            // Parse the AfterShip response structure
            if (data.TryGetProperty("data", out var dataElement) && 
                dataElement.TryGetProperty("trackings", out var trackingsElement))
            {
                var trackings = trackingsElement.EnumerateArray();
                
                if (trackings.MoveNext())
                {
                    var tracking = trackings.Current;
                    
                    // Parse tracking details
                    if (tracking.TryGetProperty("tracking_number", out var tn))
                        response.TrackingNumber = tn.GetString() ?? trackingNumber;
                    
                    if (tracking.TryGetProperty("slug", out var slug))
                        response.Carrier = slug.GetString() ?? carrier;
                    
                    if (tracking.TryGetProperty("tag", out var tag))
                        response.Status = tag.GetString() ?? "Unknown";
                    
                    if (tracking.TryGetProperty("destination_raw_location", out var location))
                        response.CurrentLocation = location.GetString() ?? "";
                    
                    if (tracking.TryGetProperty("expected_delivery", out var ed))
                    {
                        if (DateTime.TryParse(ed.GetString(), out var estimatedDate))
                            response.EstimatedDelivery = estimatedDate;
                    }
                    
                    if (tracking.TryGetProperty("shipment_delivery_date", out var dt))
                    {
                        if (DateTime.TryParse(dt.GetString(), out var deliveredDate))
                        {
                            response.DeliveredAt = deliveredDate;
                            response.IsDelivered = true;
                        }
                    }
                    
                    // Parse tracking events
                    if (tracking.TryGetProperty("checkpoints", out var checkpoints))
                    {
                        foreach (var checkpoint in checkpoints.EnumerateArray())
                        {
                            var trackingEvent = new TrackingEvent();
                            
                            if (checkpoint.TryGetProperty("checkpoint_time", out var time))
                            {
                                if (DateTime.TryParse(time.GetString(), out var eventTime))
                                    trackingEvent.Timestamp = eventTime;
                            }
                            
                            if (checkpoint.TryGetProperty("location", out var eventLocation))
                                trackingEvent.Location = eventLocation.GetString() ?? "";
                            
                            if (checkpoint.TryGetProperty("tag", out var eventStatus))
                                trackingEvent.Status = eventStatus.GetString() ?? "";
                            
                            if (checkpoint.TryGetProperty("message", out var description))
                                trackingEvent.Description = description.GetString() ?? "";
                            
                            response.Events.Add(trackingEvent);
                        }
                    }
                    
                    // Check for exceptions
                    if (tracking.TryGetProperty("exception", out var exception))
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
            _logger.LogError(ex, "Error parsing AfterShip response for {TrackingNumber}", trackingNumber);
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
