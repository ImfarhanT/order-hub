namespace HubApi.Services;

public interface ICryptoService
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
}
