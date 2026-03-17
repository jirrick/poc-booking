using Microsoft.EntityFrameworkCore;

namespace PocBooking.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<NotificationInbox> NotificationInbox => Set<NotificationInbox>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationInbox>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.NotificationUuid).IsUnique();
            e.HasIndex(x => x.MessageId).IsUnique();
        });
    }
}
