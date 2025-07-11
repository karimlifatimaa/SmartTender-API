namespace SmartTender.Domain
{
    public class ContactPerson
    {
        public int Id { get; set; }
        public int TenderId { get; set; }
        public Tender Tender { get; set; }
        public string? FullName { get; set; }
        public string? Contact { get; set; }
        public string? Position { get; set; }
        public string? PhoneNumber { get; set; }
    }
} 