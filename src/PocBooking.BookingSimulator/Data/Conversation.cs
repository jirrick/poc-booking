namespace PocBooking.BookingSimulator.Data;

public class Conversation
{
    public int Id { get; set; }
    public string ConversationId { get; set; } = string.Empty;
    public string ConversationReference { get; set; } = string.Empty;
    public string ConversationType { get; set; } = "reservation"; // reservation | request_to_book

    public int PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
