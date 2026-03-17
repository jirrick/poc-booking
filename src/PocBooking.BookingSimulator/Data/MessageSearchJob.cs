namespace PocBooking.BookingSimulator.Data;

/// <summary>Stores a message search job for GET /messaging/messages/search/result/{jobId}.</summary>
public class MessageSearchJob
{
    public int Id { get; set; }
    public string JobId { get; set; } = string.Empty;
    public DateTime? AfterUtc { get; set; }
    public DateTime? BeforeUtc { get; set; }
    public string? PropertyId { get; set; }
    public string OrderBy { get; set; } = "desc";
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
