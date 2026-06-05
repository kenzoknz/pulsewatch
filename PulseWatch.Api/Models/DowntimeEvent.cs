namespace PulseWatch.Api.Models
{
    public class DowntimeEvent
    {
        public int Id { get; set; }
        public int WebsiteId { get; set; }
        public Website Website { get; set; } = null!;
        public DateTime StartTime { get; set; }
        public DateTime? EndedAt { get; set; }
         public string? Reason { get; set; }
    }
}