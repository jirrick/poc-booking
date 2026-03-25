namespace PocBooking.BookingSimulator.Data;

public class Conversation
{
    public int Id { get; set; }
    public string ConversationId { get; set; } = string.Empty;
    public string ConversationReference { get; set; } = string.Empty;
    public string ConversationType { get; set; } = "reservation"; // reservation | request_to_book
    public bool NoReplyNeeded { get; set; } = false;

    public int PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    /// <summary>The guest participant for this conversation. Webhooks sent from this conversation use this participant as sender.</summary>
    public int? GuestParticipantId { get; set; }
    public Participant? GuestParticipant { get; set; }

    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
