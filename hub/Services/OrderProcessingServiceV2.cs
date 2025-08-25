using System.Text.Json;
using HubApi.Data;
using HubApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HubApi.Services;

/// <summary>
/// Simplified order processing service that stores data exactly as received
/// </summary>
public class OrderProcessingServiceV2 : IOrderProcessingService
{
    private readonly OrderHubDbContext _context;
    private readonly ILogger<OrderProcessingServiceV2> _logger;

    public OrderProcessingServiceV2(OrderHubDbContext context, ILogger<OrderProcessingServiceV2> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<OrderProcessingResult> ProcessRawOrderDataAsync(RawOrderData rawOrderData)
    {
        try
        {
            _logger.LogInformation("Processing raw order data {RawOrderId} from site {SiteName}", 
                rawOrderData.Id, rawOrderData.SiteName);

            // Parse the JSON
            var jsonDoc = JsonDocument.Parse(rawOrderData.RawJson);
            var root = jsonDoc.RootElement;

            // Extract API key to find the site
            if (!root.TryGetProperty("api_key", out var apiKeyProp))
            {
                return OrderProcessingResult.CreateFailure("Missing api_key in JSON payload", rawOrderData.Id);
            }

            var apiKey = apiKeyProp.GetString();
            if (string.IsNullOrEmpty(apiKey))
            {
                return OrderProcessingResult.CreateFailure("API key is empty", rawOrderData.Id);
            }

            // Find the site
            var site = await _context.Sites
                .FirstOrDefaultAsync(s => s.ApiKey == apiKey && s.IsActive);

            if (site == null)
            {
                return OrderProcessingResult.CreateFailure($"Site not found for API key: {apiKey}", rawOrderData.Id);
            }

            // Extract order data
            if (!root.TryGetProperty("order", out var orderProp))
            {
                return OrderProcessingResult.CreateFailure("Missing order data in JSON payload", rawOrderData.Id);
            }

            // Check if order already exists
            var wcOrderId = GetStringProperty(orderProp, "wc_order_id");
            if (string.IsNullOrEmpty(wcOrderId))
            {
                return OrderProcessingResult.CreateFailure("Missing wc_order_id in order data", rawOrderData.Id);
            }

            var existingOrder = await _context.OrdersV2
                .FirstOrDefaultAsync(o => o.SiteId == site.Id && o.WcOrderId == wcOrderId);

            if (existingOrder != null)
            {
                // Update existing order
                return await UpdateExistingOrderAsync(existingOrder, orderProp, rawOrderData.Id);
            }
            else
            {
                // Create new order
                return await CreateNewOrderAsync(site.Id, orderProp, rawOrderData.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing raw order data {RawOrderId}. Raw JSON: {RawJson}", 
                rawOrderData.Id, rawOrderData.RawJson);
            return OrderProcessingResult.CreateFailure($"Error processing order: {ex.Message}", rawOrderData.Id);
        }
    }

    private async Task<OrderProcessingResult> CreateNewOrderAsync(Guid siteId, JsonElement orderProp, Guid rawOrderDataId)
    {
        try
        {
            _logger.LogInformation("Creating new order for site {SiteId} from raw data {RawOrderId}", siteId, rawOrderDataId);
            
            var order = new OrderV2
            {
                Id = Guid.NewGuid(),
                SiteId = siteId,
                WcOrderId = GetStringProperty(orderProp, "wc_order_id"),
                Status = GetStringProperty(orderProp, "status"),
                Currency = GetStringProperty(orderProp, "currency"),
                OrderTotal = GetStringProperty(orderProp, "order_total"),
                Subtotal = GetStringProperty(orderProp, "subtotal"),
                DiscountTotal = GetStringProperty(orderProp, "discount_total"),
                ShippingTotal = GetStringProperty(orderProp, "shipping_total"),
                TaxTotal = GetStringProperty(orderProp, "tax_total"),
                PaymentGatewayCode = GetStringProperty(orderProp, "payment_gateway_code"),
                CustomerName = GetStringProperty(orderProp, "customer_name"),
                CustomerEmail = GetStringProperty(orderProp, "customer_email"),
                CustomerPhone = GetStringProperty(orderProp, "customer_phone"),
                ShippingAddress = GetAddressProperty(orderProp, "shipping_address"),
                BillingAddress = GetAddressProperty(orderProp, "billing_address"),
                PlacedAt = GetStringProperty(orderProp, "placed_at"),
                SyncedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Order object created: WcOrderId={WcOrderId}, Status={Status}, Total={Total}", 
                order.WcOrderId, order.Status, order.OrderTotal);

            _context.OrdersV2.Add(order);

            // Process order items
            await ProcessOrderItemsAsync(order, rawOrderDataId);

            // Mark raw data as processed
            await MarkRawDataAsProcessedAsync(rawOrderDataId);

            _logger.LogInformation("Saving changes to database...");
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new order {OrderId} from raw data {RawOrderId}", 
                order.Id, rawOrderDataId);

            return OrderProcessingResult.CreateSuccess(
                $"Order {order.WcOrderId} created successfully", 
                order.Id, 
                rawOrderDataId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating new order from raw data {RawOrderId}. Order details: SiteId={SiteId}, WcOrderId={WcOrderId}", 
                rawOrderDataId, siteId, GetStringProperty(orderProp, "wc_order_id"));
            return OrderProcessingResult.CreateFailure($"Error creating order: {ex.Message}", rawOrderDataId);
        }
    }

    private async Task<OrderProcessingResult> UpdateExistingOrderAsync(OrderV2 existingOrder, JsonElement orderProp, Guid rawOrderDataId)
    {
        try
        {
            // Update order properties
            existingOrder.Status = GetStringProperty(orderProp, "status");
            existingOrder.OrderTotal = GetStringProperty(orderProp, "order_total");
            existingOrder.Subtotal = GetStringProperty(orderProp, "subtotal");
            existingOrder.DiscountTotal = GetStringProperty(orderProp, "discount_total");
            existingOrder.ShippingTotal = GetStringProperty(orderProp, "shipping_total");
            existingOrder.TaxTotal = GetStringProperty(orderProp, "tax_total");
            existingOrder.PaymentGatewayCode = GetStringProperty(orderProp, "payment_gateway_code");
            existingOrder.CustomerName = GetStringProperty(orderProp, "customer_name");
            existingOrder.CustomerEmail = GetStringProperty(orderProp, "customer_email");
            existingOrder.CustomerPhone = GetStringProperty(orderProp, "customer_phone");
            existingOrder.ShippingAddress = GetAddressProperty(orderProp, "shipping_address");
            existingOrder.BillingAddress = GetAddressProperty(orderProp, "billing_address");
            existingOrder.SyncedAt = DateTime.UtcNow;

            // Remove existing items and add new ones
                            var existingItems = await _context.OrderItemsV2
                    .Where(oi => oi.OrderId == existingOrder.Id)
                    .ToListAsync();

            _context.OrderItemsV2.RemoveRange(existingItems);

            // Process order items
            await ProcessOrderItemsAsync(existingOrder, rawOrderDataId);

            // Mark raw data as processed
            await MarkRawDataAsProcessedAsync(rawOrderDataId);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated existing order {OrderId} from raw data {RawOrderId}", 
                existingOrder.Id, rawOrderDataId);

            return OrderProcessingResult.CreateSuccess(
                $"Order {existingOrder.WcOrderId} updated successfully", 
                existingOrder.Id, 
                rawOrderDataId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating existing order from raw data {RawOrderId}", rawOrderDataId);
            return OrderProcessingResult.CreateFailure($"Error updating order: {ex.Message}", rawOrderDataId);
        }
    }

    private async Task ProcessOrderItemsAsync(OrderV2 order, Guid rawOrderDataId)
    {
        try
        {
            _logger.LogInformation("Processing order items for order {OrderId}", order.Id);
            
            var jsonDoc = JsonDocument.Parse(_context.RawOrderData
                .First(r => r.Id == rawOrderDataId).RawJson);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("items", out var itemsProp) && itemsProp.ValueKind == JsonValueKind.Array)
            {
                var itemCount = itemsProp.GetArrayLength();
                _logger.LogInformation("Found {ItemCount} items to process", itemCount);
                
                foreach (var itemElement in itemsProp.EnumerateArray())
                {
                    var productId = GetStringProperty(itemElement, "product_id");
                    var name = GetStringProperty(itemElement, "name");
                    var qty = GetStringProperty(itemElement, "qty");
                    var price = GetStringProperty(itemElement, "price");
                    
                    _logger.LogInformation("Processing item: ProductId={ProductId}, Name={Name}, Qty={Qty}, Price={Price}", 
                        productId, name, qty, price);
                    
                    var orderItem = new OrderItemV2
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        ProductId = productId,
                        Sku = GetStringProperty(itemElement, "sku"),
                        Name = name,
                        Qty = qty,
                        Price = price,
                        Subtotal = GetStringProperty(itemElement, "subtotal"),
                        Total = GetStringProperty(itemElement, "total")
                    };

                    _context.OrderItemsV2.Add(orderItem);
                    _logger.LogInformation("Order item added: {ItemId}", orderItem.Id);
                }
            }
            else
            {
                _logger.LogWarning("No items found in JSON data for raw order {RawOrderId}", rawOrderDataId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing order items for raw data {RawOrderId}", rawOrderDataId);
            throw;
        }
    }

    private async Task MarkRawDataAsProcessedAsync(Guid rawOrderDataId)
    {
        var rawData = await _context.RawOrderData.FindAsync(rawOrderDataId);
        if (rawData != null)
        {
            rawData.Processed = true;
            rawData.ProcessedAt = DateTime.UtcNow;
        }
    }

    // Helper methods for extracting JSON properties - now everything is stored as text
    private string GetStringProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop))
        {
            // Convert any type to string
            if (prop.ValueKind == JsonValueKind.String)
                return prop.GetString() ?? string.Empty;
            else if (prop.ValueKind == JsonValueKind.Number)
                return prop.GetDecimal().ToString();
            else if (prop.ValueKind == JsonValueKind.True || prop.ValueKind == JsonValueKind.False)
                return prop.GetBoolean().ToString();
            else if (prop.ValueKind == JsonValueKind.Null)
                return string.Empty;
        }
        return string.Empty;
    }

    private JsonDocument? GetAddressProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Object)
        {
            return JsonDocument.Parse(prop.GetRawText());
        }
        return null;
    }

    // Implement missing interface methods
    public async Task<OrderProcessingResult> ProcessRawOrderDataAsync(Guid rawOrderDataId)
    {
        var rawData = await _context.RawOrderData.FindAsync(rawOrderDataId);
        if (rawData == null)
        {
            return OrderProcessingResult.CreateFailure($"Raw order data not found: {rawOrderDataId}", rawOrderDataId);
        }
        
        return await ProcessRawOrderDataAsync(rawData);
    }

    public async Task<List<OrderProcessingResult>> ProcessAllUnprocessedRawDataAsync()
    {
        var results = new List<OrderProcessingResult>();
        var unprocessedData = await _context.RawOrderData
            .Where(r => !r.Processed)
            .ToListAsync();

        foreach (var rawData in unprocessedData)
        {
            var result = await ProcessRawOrderDataAsync(rawData);
            results.Add(result);
        }

        return results;
    }
}
