using DeligateWebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace DeligateWebAPI.Data
{
    public class ChatContext : DbContext
    {
        public ChatContext(DbContextOptions<ChatContext> options) : base(options) { }

        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<ChatRoom> ChatRooms { get; set; }

        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    modelBuilder.Entity<ChatRoom>()
        //        .Property(e => e.ParticipantIds)
        //        .HasConversion(
        //            v => string.Join(',', v),
        //            v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
        //        );
        //}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // PostgreSQL specific configurations
            modelBuilder.Entity<ChatMessage>()
                .Property(e => e.Id)
                .UseIdentityByDefaultColumn(); // PostgreSQL identity column

            // Your existing configurations...
        }
    }
}
