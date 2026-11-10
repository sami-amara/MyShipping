using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOS
{
    public class ShipmentTimelineEventDto
    {
        public Guid Id { get; set; }
        public Guid? ShipmentId { get; set; }
        public string TrackingNumber { get; set; }
        public string ShipmentEventType { get; set; }
        public string Severity { get; set; }
        public DateTime Timestamp { get; set; }
        public string? UserId { get; set; }
        public string Email { get; set; }
        public string? UserDisplayName { get; set; }
        public string IpAddress { get; set; }
        public string? Description { get; set; }
        public string? Reason { get; set; }
        public string? Metadata { get; set; }

        public string FormattedTime => Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
        public string TimeAgo => GetTimeAgo(Timestamp);
        public string RelativeTime => TimeAgo;

        private static string GetTimeAgo(DateTime timestamp)
        {
            var timeSpan = DateTime.UtcNow - timestamp;
            if (timeSpan.TotalSeconds < 60) return "Just now";
            if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes}m ago";
            if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours}h ago";
            if (timeSpan.TotalDays < 30) return $"{(int)timeSpan.TotalDays}d ago";
            return timestamp.ToString("MMM dd, yyyy");
        }
    }
}
