using System.Security.Cryptography;
using System.Text;

namespace AutVent.CorePlatform.Api.Common.Security;

public static class PasswordHasher
{
    public static string Hash(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}
