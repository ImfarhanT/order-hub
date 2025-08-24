namespace HubApi.Services;

public interface IHmacService
{
    string ComputeSignature(string signatureBase, string secret);
    bool VerifySignature(string signatureBase, string signature, string secret);
}
