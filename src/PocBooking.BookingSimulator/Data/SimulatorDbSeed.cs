using Microsoft.EntityFrameworkCore;

namespace PocBooking.BookingSimulator.Data;

public static class SimulatorDbSeed
{
    public static async Task SeedAsync(SimulatorDbContext db, CancellationToken ct = default)
    {
        if (await db.Properties.AnyAsync(ct))
            return;

        var prop = new Property
        {
            PropertyId = "1383087",
            Name = "Test Property"
        };
        db.Properties.Add(prop);
        await db.SaveChangesAsync(ct);

        var guest = new Participant
        {
            ParticipantId = Guid.NewGuid().ToString(),
            Name = "Test Guest",
            ParticipantType = "guest",
            PropertyId = prop.Id
        };
        var hotel = new Participant
        {
            ParticipantId = "9f6be5fd-b3a8-5691-9cf9-9ab6c6217327",
            Name = "Test Property",
            ParticipantType = "hotel",
            PropertyId = prop.Id
        };
        db.Participants.AddRange(guest, hotel);
        await db.SaveChangesAsync(ct);

        var conv = new Conversation
        {
            ConversationId = "f3a9c29d-480d-5f5b-a6c0-65451e335353",
            ConversationReference = "3812391309",
            ConversationType = "reservation",
            PropertyId = prop.Id
        };
        db.Conversations.Add(conv);
        await db.SaveChangesAsync(ct);

        var firstMessage = new Message
        {
            MessageId = Guid.NewGuid().ToString(),
            Content = "Welcome! How can we help?",
            MessageType = "free_text",
            TimestampUtc = DateTime.UtcNow.AddMinutes(-10),
            ConversationId = conv.Id,
            SenderParticipantId = hotel.Id
        };
        db.Messages.Add(firstMessage);
        await db.SaveChangesAsync(ct);
    }
}
