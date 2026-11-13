using System.Security.Cryptography;
using System.Text;

namespace AnimalFitness.Infrastructure.Security;

/// <summary>
/// Genera tokens de invitación y su hash. El token en claro nunca se persiste.
/// </summary>
public static class InvitationToken
{
    /// <summary>Crea un token URL-safe y su hash SHA-256 en hex.</summary>
    public static (string PlainToken, string Hash) Create()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var plain = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return (plain, Hash(plain));
    }

    /// <summary>Calcula el hash del token en claro.</summary>
    public static string Hash(string plainToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainToken);
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(plainToken.Trim()));
        return Convert.ToHexString(digest).ToLowerInvariant();
    }
}
