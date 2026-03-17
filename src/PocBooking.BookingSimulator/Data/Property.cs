namespace PocBooking.BookingSimulator.Data;

public class Property
{
    public int Id { get; set; }
    public string PropertyId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
    public ICollection<Participant> Participants { get; set; } = new List<Participant>();
}
