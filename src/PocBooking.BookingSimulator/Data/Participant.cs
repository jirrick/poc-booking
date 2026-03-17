namespace PocBooking.BookingSimulator.Data;

public class Participant
{
    public int Id { get; set; }
    public string ParticipantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ParticipantType { get; set; } = "guest"; // guest | hotel

    public int PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
