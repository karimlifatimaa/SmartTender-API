using System;
using System.Collections.Generic;

namespace SmartTender.Domain
{
    public class Tender
    {
        public int Id { get; set; }
        public int RfxId { get; set; }
        public int EventId { get; set; }
        public string TenderName { get; set; }
        public string OrganizationName { get; set; }
        public string OrganizationVoen { get; set; }
        public long EnvelopeDate { get; set; }
        public long EndDate { get; set; }
        public long PublishDate { get; set; }
        public long StartDate { get; set; }
        public string BudgetCategoryCode { get; set; }
        public string Address { get; set; }
        public string CpvCode { get; set; }
        public int EventType { get; set; }
        public bool IsRedirectionAvailable { get; set; }
        public int MinNumberOfSuppliers { get; set; }
        public decimal EstimatedAmount { get; set; }

        public ICollection<TenderCategory> TenderCategories { get; set; }
        public ICollection<BomLine> BomLines { get; set; }
        public ICollection<ContactPerson> ContactPersons { get; set; }
        public ICollection<Announcement> Announcements { get; set; }
    }
} 