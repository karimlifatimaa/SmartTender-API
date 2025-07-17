using System.Collections.Generic;

namespace SmartTender.Domain
{
    public class Category
    {
        public int Id { get; set; }
        // public int EtenderId { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }

        public ICollection<TenderCategory> TenderCategories { get; set; }
    }
} 