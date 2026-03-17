using Microsoft.EntityFrameworkCore;

namespace PocBooking.BookingSimulator.Data;

public class SimulatorDbContext(DbContextOptions<SimulatorDbContext> options) : DbContext(options)
{
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageSearchJob> MessageSearchJobs => Set<MessageSearchJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Property>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.PropertyId).IsUnique();
        });

        modelBuilder.Entity<Participant>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ParticipantId).IsUnique();
            e.HasOne(x => x.Property).WithMany(p => p.Participants).HasForeignKey(x => x.PropertyId);
        });

        modelBuilder.Entity<Conversation>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ConversationId).IsUnique();
            e.HasOne(x => x.Property).WithMany(p => p.Conversations).HasForeignKey(x => x.PropertyId);
            e.HasOne(x => x.GuestParticipant).WithMany().HasForeignKey(x => x.GuestParticipantId).IsRequired(false);
        });

        modelBuilder.Entity<Message>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.MessageId).IsUnique();
            e.HasOne(x => x.Conversation).WithMany(c => c.Messages).HasForeignKey(x => x.ConversationId);
            e.HasOne(x => x.Sender).WithMany(p => p.Messages).HasForeignKey(x => x.SenderParticipantId);
        });

        modelBuilder.Entity<MessageSearchJob>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.JobId).IsUnique();
        });
    }
}
