namespace SmartTender.Domain
{
    public class TenderCategory
    {
        public int TenderId { get; set; }
        public Tender Tender { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; }
    }
} 