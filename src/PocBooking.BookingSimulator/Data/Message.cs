namespace PocBooking.BookingSimulator.Data;

public class Message
{
    public int Id { get; set; }
    public string MessageId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string MessageType { get; set; } = "free_text";
    public DateTime TimestampUtc { get; set; }

    public int ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;

    public int SenderParticipantId { get; set; }
    public Participant Sender { get; set; } = null!;
}
