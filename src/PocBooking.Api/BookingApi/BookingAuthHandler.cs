using System.Net.Http.Headers;
using Microsoft.Extensions.Options;

namespace PocBooking.Api.BookingApi;

/// <summary>
/// DelegatingHandler that attaches a Bearer token to every outgoing Booking messaging API request.
/// Priority: live JWT from <see cref="IBookingTokenStore"/> (when valid) → static <c>Booking:ApiKey</c>.
/// </summary>
public sealed class BookingAuthHandler : DelegatingHandler
{
    private readonly IBookingTokenStore _tokenStore;
    private readonly IOptions<BookingApiOptions> _options;

    public BookingAuthHandler(IBookingTokenStore tokenStore, IOptions<BookingApiOptions> options)
    {
        _tokenStore = tokenStore;
        _options = options;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var token = _tokenStore.IsValid
            ? _tokenStore.Jwt
            : _options.Value.ApiKey;

        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return base.SendAsync(request, ct);
    }
}

