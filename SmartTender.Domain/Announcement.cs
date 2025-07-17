namespace SmartTender.Domain
{
    public class Announcement
    {
        public int Id { get; set; }
        // public int EtenderId { get; set; }
        public int TenderId { get; set; }
        public Tender Tender { get; set; }
        public int AnnouncementVersion { get; set; }
        public string? Text { get; set; }
    }
} 