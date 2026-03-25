using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace PocBooking.BookingSimulator.Services;

/// <summary>
/// Singleton that owns a single ephemeral RSA-2048 key pair used to sign simulated
/// Booking.com connectivity JWTs (RS256, kid="1").
/// The key is regenerated each time the application starts — this is intentional for a simulator.
/// </summary>
public sealed class SimulatorRsaKeyProvider : IDisposable
{
    private readonly RSA _rsa = RSA.Create(2048);

    /// <summary>Returns the signing key (includes private material). Used when issuing tokens.</summary>
    public RsaSecurityKey GetSigningKey() => new(_rsa) { KeyId = "1" };

    /// <summary>Exports the public key as a <see cref="JsonWebKey"/> for the JWKS endpoint.</summary>
    public JsonWebKey GetPublicJwk()
    {
        var publicKey = new RsaSecurityKey(_rsa.ExportParameters(includePrivateParameters: false)) { KeyId = "1" };
        var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(publicKey);
        jwk.Use = "sig";
        jwk.Alg = SecurityAlgorithms.RsaSha256;
        return jwk;
    }

    public void Dispose() => _rsa.Dispose();
}

