namespace HubApi.Services;

public interface ITrackingService
{
    Task<TrackingResponse> GetLiveTrackingAsync(string trackingNumber, string carrier);
    Task<List<TrackingResponse>> GetBulkTrackingAsync(List<string> trackingNumbers);
    Task<bool> IsTrackingNumberValidAsync(string trackingNumber);
}

public class TrackingResponse
{
    public string TrackingNumber { get; set; } = string.Empty;
    public string Carrier { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CurrentLocation { get; set; } = string.Empty;
    public DateTime? EstimatedDelivery { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public List<TrackingEvent> Events { get; set; } = new();
    public bool IsDelivered { get; set; }
    public bool HasException { get; set; }
    public string ExceptionMessage { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class TrackingEvent
{
    public DateTime Timestamp { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
