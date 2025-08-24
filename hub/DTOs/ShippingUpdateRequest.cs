using System.Text.Json;

namespace HubApi.DTOs;

public class ShippingUpdateRequest
{
    public string SiteApiKey { get; set; } = string.Empty;
    public string Nonce { get; set; } = string.Empty;
    public long Timestamp { get; set; }
    public string Signature { get; set; } = string.Empty;
    public string WcOrderId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string? TrackingNumber { get; set; }
    public JsonDocument? Payload { get; set; }
    public DateTime OccurredAt { get; set; }
}
