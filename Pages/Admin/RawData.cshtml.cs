using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HubApi.Data;
using HubApi.Models;
using System;

namespace HubApi.Pages.Admin
{
    public class RawDataModel : PageModel
    {
        private readonly OrderHubDbContext _context;
        private readonly ILogger<RawDataModel> _logger;

        public RawDataModel(OrderHubDbContext context, ILogger<RawDataModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<RawOrderData> RawOrders { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalPages { get; set; }
        public bool HasMoreRawData => RawOrders.Count < TotalCount;
        public int CurrentPage => PageNumber;

        public async Task OnGetAsync(int page = 1, int pageSize = 20)
        {
            PageNumber = page;
            PageSize = Math.Max(1, Math.Min(100, pageSize)); // Limit page size between 1 and 100
            
            _logger.LogInformation("RawData OnGetAsync called with page={PageNumber}, pageSize={PageSize}", PageNumber, PageSize);

            var query = _context.RawOrderData
                .Include(r => r.Site)
                .OrderByDescending(r => r.ReceivedAt);

            TotalCount = await query.CountAsync();
            TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);

            RawOrders = await query
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }

        public async Task<RedirectToPageResult> OnGetDeleteAsync(Guid id)
        {
            var rawData = await _context.RawOrderData.FindAsync(id);
            if (rawData != null)
            {
                _context.RawOrderData.Remove(rawData);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Raw order data deleted: {Id}", id);
            }

            return RedirectToPage();
        }

        public async Task<RedirectToPageResult> OnGetMarkProcessedAsync(Guid id)
        {
            var rawData = await _context.RawOrderData.FindAsync(id);
            if (rawData != null)
            {
                rawData.Processed = true;
                rawData.ProcessedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Raw order data marked as processed: {Id}", id);
            }

            return RedirectToPage();
        }
    }
}
