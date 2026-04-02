namespace PocBooking.BookingSimulator.Services;

public record GeneratedTestMessage(string FullText, string ExpectedText);

public class BookingMessageTestGenerator
{
    private readonly Random _random = new();

    private static readonly string[] Subjects =
    [
        "Re: You have a message from Grand Plaza Hotel (8821)",
        "Re: You have a message from Seaside Resort (5567)",
        "Re: You have a message from Mountain Lodge (3344)",
        "Re: You have a message from City Center Inn (7712)",
        "Re: You have a message from Boutique Residence Prague (2290)"
    ];

    private static readonly string[] GuestMessages =
    [
        "Hi, we will be arriving around 10 PM. Is late check-in possible?",
        "Could you please arrange an airport transfer for two guests?",
        "We need a baby crib in the room, thank you.",
        "Yes that works, please book the spa for 3 PM.",
        "Thanks!",
        "Hello,\n\nWe are a group of 4 and we'd like to know:\n1. Is parking available on site?\n2. Do you serve breakfast before 7 AM?\n3. Can we store our ski equipment somewhere?\n\nThank you,\nPetr",
        "No, we won't need anything extra. See you on Friday!",
        "Can we get a room with a view of the river instead?",
        "Sorry for the late reply. Yes, we confirm the reservation for 3 nights.",
        "Is it possible to check out at 2 PM instead of 11? We have a late flight."
    ];

    private static readonly string[] HotelMessages =
    [
        "Hello, do you have any special requests?",
        "Would you like to book a spa session during your stay?",
        "Hi, feel free to ask if you need anything before your trip!",
        "Your room has been upgraded to a suite. Enjoy your stay!",
        "We wanted to confirm your arrival time. Could you let us know?",
        "Dear guest, breakfast is served from 7 AM to 10 AM daily.",
        "Please note that parking is available for an additional fee of 15 EUR/day.",
        "We look forward to welcoming you. Do you need airport transfer arrangements?"
    ];

    private static readonly string[] ConversationHistorySnippets =
    [
        "Welcome! We look forward to your stay.",
        "Your reservation is confirmed. See you soon!",
        "We have good news about your reservation.",
        "Thank you for choosing our hotel.",
        "Reminder: your check-in is tomorrow."
    ];

    private static readonly string[] PropertyNames =
    [
        "Grand Plaza Hotel (8821)",
        "Seaside Resort (5567)",
        "Mountain Lodge (3344)",
        "City Center Inn (7712)",
        "Boutique Residence Prague (2290)"
    ];

    private static readonly string[] GuestEmails =
    [
        "jannovak@seznam.cz",
        "petr.svoboda@gmail.com",
        "martin.dvorak@outlook.cz",
        "maria.king@yahoo.com",
        "zhang.wei@hotmail.com"
    ];

    public GeneratedTestMessage Generate()
    {
        var guestMessage = Pick(GuestMessages);
        var includeSubject = _random.NextDouble() < 0.75;
        var includeQuotedBody = _random.NextDouble() < 0.75;

        if (!includeSubject && !includeQuotedBody)
            return new GeneratedTestMessage(guestMessage, guestMessage);

        var parts = new List<string>();

        if (includeSubject)
            parts.Add(Pick(Subjects));

        parts.Add(guestMessage);

        if (includeQuotedBody)
            parts.Add(BuildQuotedBody());

        var fullText = string.Join("\n\n", parts);
        return new GeneratedTestMessage(fullText, guestMessage);
    }

    public IEnumerable<GeneratedTestMessage> Generate(int count)
        => Enumerable.Range(0, count).Select(_ => Generate());

    private string BuildQuotedBody()
    {
        var property = Pick(PropertyNames);
        var bookingNumber = _random.Next(1_000_000_000, int.MaxValue).ToString();
        var guestEmail = Pick(GuestEmails);
        var hotelMessage = Pick(HotelMessages);
        var useCzechHeaders = _random.NextDouble() < 0.4;

        var parts = new List<string>();

        if (useCzechHeaders)
        {
            parts.Add(
                $"Od: {property} through Booking.com\n" +
                $"<{bookingNumber}-ab12.cd34.ef56.gh78@property.booking.com>\n" +
                $"Odesláno: {FormatCzechDate()}\n" +
                $"Komu: {guestEmail} <{guestEmail}>\n" +
                $"Předmět: You have a message from {property}");
        }
        else
        {
            parts.Add(
                $"From: {property} through Booking.com\n" +
                $"Sent: {FormatEnglishDate()}\n" +
                $"To: {guestEmail}\n" +
                $"Subject: You have a message from {property}");
        }

        parts.Add(
            $"[1]booking.com            Confirmation number: [2]{bookingNumber}\n\n" +
            $"You have a new message from {property}\n\n" +
            $"{property} said:\n\n" +
            $" {hotelMessage}\n\n" +
            "Just now\n\n" +
            " Please respond to the property by replying to this email.");

        if (_random.NextDouble() < 0.6)
        {
            var historyEntry = Pick(ConversationHistorySnippets);
            var historyDate = DateTime.Today.AddDays(-_random.Next(1, 5));
            parts.Add(
                $"Most recent messages\n" +
                $"{property}\n" +
                $"{historyEntry}\n" +
                $"{historyDate:dd MMM yyyy} {_random.Next(8, 18)}:{_random.Next(0, 60):D2}");
        }

        parts.Add(
            "Reservation Details\n\n" +
            $"Check-in:                      Check-out:\n" +
            $"{DateTime.Today:ddd d MMM yyyy}                 {DateTime.Today.AddDays(_random.Next(1, 8)):ddd d MMM yyyy}\n\n" +
            $"Property Name:\n[3]{property}\n\n" +
            $"Booking Number:\n[4]{bookingNumber}");

        parts.Add(
            "© Copyright [5]Booking.com 2026\n" +
            "This email was sent by [6]Booking.com\n" +
            "Booking.com will receive and process replies to this email\n" +
            "as set forth in the[7]Booking.com Privacy Statement. The\n" +
            $"content of the message from {property} was\n" +
            "not generated by Booking.com, meaning Booking.com cannot be\n" +
            "held accountable for the content of the message.\n" +
            "[8]Why did I receive this message?");

        parts.Add(
            "References\n\n" +
            "   Visible links\n" +
            "   1. [link removed]\n" +
            $"   2. https://secure.booking.com/myreservations.html?bn={bookingNumber}\n" +
            $"   3. https://www.booking.com/hotel/cz/{property.ToLowerInvariant().Replace(' ', '-')}.html\n" +
            $"   4. https://secure.booking.com/myreservations.html?bn={bookingNumber}\n" +
            "   5. https://www.booking.com/?source=guest_email\n" +
            "   6. https://www.booking.com/?source=guest_email\n" +
            "   7. https://www.booking.com/content/privacy.html\n" +
            "   8. https://secure.booking.com/faq.html?faq_item=item_communication");

        return string.Join("\n \n", parts);
    }

    private string FormatCzechDate()
    {
        string[] czechDays = ["neděle", "pondělí", "úterý", "středa", "čtvrtek", "pátek", "sobota"];
        string[] czechMonths = ["", "ledna", "února", "března", "dubna", "května", "června",
                                "července", "srpna", "září", "října", "listopadu", "prosince"];
        var date = DateTime.Today;
        return $"{czechDays[(int)date.DayOfWeek]} {date.Day}. {czechMonths[date.Month]} {date.Year} " +
               $"{_random.Next(8, 18)}:{_random.Next(0, 60):D2}";
    }

    private string FormatEnglishDate()
    {
        var date = DateTime.Today;
        return $"{date:dddd, MMMM d, yyyy} {_random.Next(8, 18)}:{_random.Next(0, 60):D2} {(_random.NextDouble() < 0.5 ? "AM" : "PM")}";
    }

    private T Pick<T>(T[] array) => array[_random.Next(array.Length)];
}