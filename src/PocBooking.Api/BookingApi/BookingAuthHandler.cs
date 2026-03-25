using System.Net.Http.Headers;

namespace PocBooking.Api.BookingApi;

/// <summary>
/// Attaches the live connectivity JWT from <see cref="IBookingTokenStore"/> as a Bearer token
/// on every outgoing Booking messaging API request.
/// If no valid token is present the request is sent without an Authorization header.
/// </summary>
public sealed class BookingAuthHandler : DelegatingHandler
{
    private readonly IBookingTokenStore _tokenStore;

    public BookingAuthHandler(IBookingTokenStore tokenStore)
    {
        _tokenStore = tokenStore;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (_tokenStore.IsValid)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenStore.Jwt);

        return base.SendAsync(request, ct);
    }
}
