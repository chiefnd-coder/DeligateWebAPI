using DeligateWebAPI.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace DeligateWebAPI.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Grocery> Groceries { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Media> Medias { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<Register> Register { get; set; }
        public DbSet<Onboarding> Onboarding { get; set; }
        public DbSet<RegisterArchive> RegisterArchive { get; set; }
        public DbSet<DeletedAccount> DeletedAccount { get; set; }
        public DbSet<UserTracker> UserTracker { get; set; }
        
        public DbSet<VendorRegistration> VendorRegistration { get; set; }
        public DbSet<DeligateTask> DeligateTasks { get; set; }
        public DbSet<People> Peoples { get; set; }
        public DbSet<UserTasks> UserTasks { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<VendorSubcategory> VendorSubcategories { get; set; }
        public DbSet<VendorCategory> VendorCategories { get; set; }

        public DbSet<DeligateWebAPI.Models.Designation> Designation { get; set; } = default!;
        public DbSet<DeligateWebAPI.Models.VendorRating> VendorRating { get; set; } = default!;
        public DbSet<DeligateWebAPI.Models.VendorTasks> VendorTasks { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Configure Message entity
            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SenderId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ReceiverId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.Timestamp).IsRequired();
                entity.HasIndex(e => new { e.SenderId, e.ReceiverId, e.Timestamp });
            });
        }
    }
}
