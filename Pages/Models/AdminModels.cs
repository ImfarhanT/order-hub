namespace HubApi.Pages.Models;

public class OrderSummary
{
    public Guid Id { get; set; }
    public string WcOrderId { get; set; } = string.Empty;
    public string SiteName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal OrderTotal { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime PlacedAt { get; set; }
}

public class SiteStat
{
    public string SiteName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal TotalRevenue { get; set; }
}
