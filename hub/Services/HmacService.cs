using System.Security.Cryptography;
using System.Text;

namespace HubApi.Services;

public class HmacService : IHmacService
{
    public string ComputeSignature(string signatureBase, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureBase));
        return Convert.ToBase64String(hash);
    }

    public bool VerifySignature(string signatureBase, string signature, string secret)
    {
        var computedSignature = ComputeSignature(signatureBase, secret);
        return string.Equals(computedSignature, signature, StringComparison.Ordinal);
    }
}
