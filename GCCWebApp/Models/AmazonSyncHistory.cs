using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Models
{
    public class AmazonSyncHistory
    {
        [Key]
        public int Id { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [StringLength(20)]
        public required string Type { get; set; } // Auto, Manual

        public int RecordsProcessed { get; set; } = 0;

        public int RecordsSucceeded { get; set; } = 0;

        public int RecordsFailed { get; set; } = 0;

        [StringLength(20)]
        public required string Status { get; set; } // success, error, partial

        public double DurationSeconds { get; set; }

        [StringLength(1000)]
        public string? Message { get; set; }

        [StringLength(4000)]
        public string? ErrorDetails { get; set; }

        [StringLength(100)]
        public string? TriggeredBy { get; set; }
    }
}
