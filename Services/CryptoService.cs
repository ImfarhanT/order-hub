using System.Security.Cryptography;
using System.Text;

namespace HubApi.Services;

public class CryptoService : ICryptoService
{
    private readonly byte[] _key;

    public CryptoService(IConfiguration configuration)
    {
        var keyBase64 = configuration["SITE_SECRETS_KEY"] 
            ?? throw new InvalidOperationException("SITE_SECRETS_KEY not configured");
        _key = Convert.FromBase64String(keyBase64);
        
        if (_key.Length != 32)
            throw new InvalidOperationException("SITE_SECRETS_KEY must be 32 bytes (256 bits)");
    }

    public string Encrypt(string plaintext)
    {
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = new byte[12];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(nonce);

        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(_key);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var result = new byte[nonce.Length + tag.Length + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, result, nonce.Length + tag.Length, ciphertext.Length);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string ciphertext)
    {
        var ciphertextBytes = Convert.FromBase64String(ciphertext);
        
        if (ciphertextBytes.Length < 28) // 12 + 16 minimum
            throw new ArgumentException("Invalid ciphertext");

        var nonce = new byte[12];
        var tag = new byte[16];
        var encryptedData = new byte[ciphertextBytes.Length - 28];

        Buffer.BlockCopy(ciphertextBytes, 0, nonce, 0, 12);
        Buffer.BlockCopy(ciphertextBytes, 12, tag, 0, 16);
        Buffer.BlockCopy(ciphertextBytes, 28, encryptedData, 0, encryptedData.Length);

        var plaintext = new byte[encryptedData.Length];

        using var aes = new AesGcm(_key);
        aes.Decrypt(nonce, encryptedData, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }
}
