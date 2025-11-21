using Microsoft.EntityFrameworkCore;
using ChatService.Models;

namespace ChatService.Data;

public class MessageDbContext : DbContext
{
    public MessageDbContext(DbContextOptions<MessageDbContext> options) : base(options) { }

    public DbSet<ChatMessage> Messages => Set<ChatMessage>();
}