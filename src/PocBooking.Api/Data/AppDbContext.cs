using Microsoft.EntityFrameworkCore;

namespace PocBooking.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<NotificationInbox> NotificationInbox => Set<NotificationInbox>();
    public DbSet<ReservationMapping> ReservationMappings => Set<ReservationMapping>();
    public DbSet<GuestMapping> GuestMappings => Set<GuestMapping>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationInbox>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.NotificationUuid).IsUnique();
            e.HasIndex(x => x.MessageId).IsUnique();
        });
        modelBuilder.Entity<ReservationMapping>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.BookingReservationId).IsUnique();
        });
        modelBuilder.Entity<GuestMapping>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.BookingGuestId).IsUnique();
        });
        modelBuilder.Entity<ProcessedMessage>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.NotificationInboxId).IsUnique();
        });
    }
}
