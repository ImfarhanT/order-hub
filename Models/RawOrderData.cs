using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HubApi.Models
{
    /// <summary>
    /// Temporary model for storing raw JSON data from websites
    /// This will be replaced with proper order parsing in the future
    /// </summary>
    public class RawOrderData
    {
        [Key]
        public Guid Id { get; set; }
        
        [Required]
        public Guid SiteId { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string SiteName { get; set; } = string.Empty;
        
        [Required]
        public string RawJson { get; set; } = string.Empty;
        
        [Required]
        public DateTime ReceivedAt { get; set; }
        
        public bool Processed { get; set; }
        
        public DateTime? ProcessedAt { get; set; }
        
        // Navigation property
        public virtual Site Site { get; set; } = null!;
    }
}
