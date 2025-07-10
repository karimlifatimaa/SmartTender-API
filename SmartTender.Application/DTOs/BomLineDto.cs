namespace SmartTender.Application.DTOs
{
    public class BomLineDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string UnitOfMeasure { get; set; }
        public int Quantity { get; set; }
        public string CategoryCode { get; set; }
    }
} 