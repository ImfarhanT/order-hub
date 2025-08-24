using System.Text.Json;

namespace HubApi.DTOs;

public class OrderSyncRequest
{
    public string SiteApiKey { get; set; } = string.Empty;
    public string Nonce { get; set; } = string.Empty;
    public long Timestamp { get; set; }
    public string Signature { get; set; } = string.Empty;
    public OrderData Order { get; set; } = new();
    public List<OrderItemData> Items { get; set; } = new();
    public decimal? GatewayFeePercent { get; set; }
    public decimal? GatewayFeeAmount { get; set; }
}

public class OrderData
{
    public string WcOrderId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public decimal OrderTotal { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal ShippingTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public string PaymentGatewayCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public JsonDocument? ShippingAddress { get; set; }
    public JsonDocument? BillingAddress { get; set; }
    public DateTime PlacedAt { get; set; }
}

public class OrderItemData
{
    public string ProductId { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public decimal Price { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
}
