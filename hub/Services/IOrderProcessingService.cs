using HubApi.Models;

namespace HubApi.Services;

public interface IOrderProcessingService
{
    Task<OrderProcessingResult> ProcessRawOrderDataAsync(Guid rawOrderDataId);
    Task<OrderProcessingResult> ProcessRawOrderDataAsync(RawOrderData rawOrderData);
    Task<List<OrderProcessingResult>> ProcessAllUnprocessedRawDataAsync();
}

