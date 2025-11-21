using Microsoft.EntityFrameworkCore;
using RealTimeConnect.Models.User;

namespace RealTimeConnect
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Conversation> Conversations => Set<Conversation>();
        public DbSet<ConversationMember> ConversationMembers => Set<ConversationMember>();
        public DbSet<Message> Messages => Set<Message>();
        public DbSet<Contact> Contacts => Set<Contact>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ConversationMember>()
                .HasKey(cm => new { cm.UserId, cm.ConversationId });
            modelBuilder.Entity<Contact>()
                .HasKey(c => new { c.OwnerUserId, c.ContactUserId });
            modelBuilder.Entity<Contact>()
                .HasOne(c => c.OwnerUser)
                .WithMany()
                .HasForeignKey(c => c.OwnerUserId);
            modelBuilder.Entity<Contact>()
                .HasOne(c => c.ContactUser)
                .WithMany()
                .HasForeignKey(c => c.ContactUserId);
        }
    }
}
