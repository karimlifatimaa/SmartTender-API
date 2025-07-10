namespace SmartTender.Domain
{
    public class BomLine
    {
        public int Id { get; set; }
        public int TenderId { get; set; }
        public Tender Tender { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string UnitOfMeasure { get; set; }
        public int Quantity { get; set; }
        public string CategoryCode { get; set; }
    }
} 