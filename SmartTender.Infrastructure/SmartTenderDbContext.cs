using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartTender.Domain;

namespace SmartTender.Infrastructure
{
    public class SmartTenderDbContext : IdentityDbContext<AppUser>
    {
        public SmartTenderDbContext(DbContextOptions<SmartTenderDbContext> options) : base(options) { }

        public DbSet<Tender> Tenders { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<TenderCategory> TenderCategories { get; set; }
        public DbSet<BomLine> BomLines { get; set; }
        public DbSet<ContactPerson> ContactPersons { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<TenderCategory>()
                .HasKey(tc => new { tc.TenderId, tc.CategoryId });

            builder.Entity<TenderCategory>()
                .HasOne(tc => tc.Tender)
                .WithMany(t => t.TenderCategories)
                .HasForeignKey(tc => tc.TenderId);

            builder.Entity<TenderCategory>()
                .HasOne(tc => tc.Category)
                .WithMany(c => c.TenderCategories)
                .HasForeignKey(tc => tc.CategoryId);
            builder.Entity<RefreshToken>()
                .HasOne(rt => rt.AppUser)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.AppUserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
} 