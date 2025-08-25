namespace HubApi.Models;

public class OrderProcessingResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? ProcessedOrderId { get; set; }
    public Guid RawOrderDataId { get; set; }
    public List<string> Errors { get; set; } = new List<string>();
    public List<string> Warnings { get; set; } = new List<string>();
    
    public static OrderProcessingResult CreateSuccess(string message, Guid processedOrderId, Guid rawOrderDataId)
    {
        return new OrderProcessingResult
        {
            Success = true,
            Message = message,
            ProcessedOrderId = processedOrderId,
            RawOrderDataId = rawOrderDataId
        };
    }
    
    public static OrderProcessingResult CreateFailure(string message, Guid rawOrderDataId, List<string> errors = null)
    {
        return new OrderProcessingResult
        {
            Success = false,
            Message = message,
            RawOrderDataId = rawOrderDataId,
            Errors = errors ?? new List<string>()
        };
    }
}

