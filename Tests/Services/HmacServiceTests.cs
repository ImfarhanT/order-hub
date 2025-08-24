using FluentAssertions;
using HubApi.Services;
using Xunit;

namespace HubApi.Tests.Services;

public class HmacServiceTests
{
    private readonly IHmacService _hmacService;

    public HmacServiceTests()
    {
        _hmacService = new HmacService();
    }

    [Fact]
    public void ComputeSignature_WithValidInput_ReturnsExpectedSignature()
    {
        // Arrange
        var signatureBase = "test_api_key|1234567890|test_nonce|order123|99.99";
        var secret = "test_secret_key";

        // Act
        var signature = _hmacService.ComputeSignature(signatureBase, secret);

        // Assert
        signature.Should().NotBeNullOrEmpty();
        signature.Should().NotBe(signatureBase);
    }

    [Fact]
    public void ComputeSignature_WithSameInput_ReturnsConsistentSignature()
    {
        // Arrange
        var signatureBase = "test_api_key|1234567890|test_nonce|order123|99.99";
        var secret = "test_secret_key";

        // Act
        var signature1 = _hmacService.ComputeSignature(signatureBase, secret);
        var signature2 = _hmacService.ComputeSignature(signatureBase, secret);

        // Assert
        signature1.Should().Be(signature2);
    }

    [Fact]
    public void VerifySignature_WithValidSignature_ReturnsTrue()
    {
        // Arrange
        var signatureBase = "test_api_key|1234567890|test_nonce|order123|99.99";
        var secret = "test_secret_key";
        var signature = _hmacService.ComputeSignature(signatureBase, secret);

        // Act
        var isValid = _hmacService.VerifySignature(signatureBase, signature, secret);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifySignature_WithInvalidSignature_ReturnsFalse()
    {
        // Arrange
        var signatureBase = "test_api_key|1234567890|test_nonce|order123|99.99";
        var secret = "test_secret_key";
        var invalidSignature = "invalid_signature";

        // Act
        var isValid = _hmacService.VerifySignature(signatureBase, invalidSignature, secret);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void VerifySignature_WithDifferentSecret_ReturnsFalse()
    {
        // Arrange
        var signatureBase = "test_api_key|1234567890|test_nonce|order123|99.99";
        var secret1 = "test_secret_key_1";
        var secret2 = "test_secret_key_2";
        var signature = _hmacService.ComputeSignature(signatureBase, secret1);

        // Act
        var isValid = _hmacService.VerifySignature(signatureBase, signature, secret2);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void VerifySignature_WithDifferentSignatureBase_ReturnsFalse()
    {
        // Arrange
        var signatureBase1 = "test_api_key|1234567890|test_nonce|order123|99.99";
        var signatureBase2 = "test_api_key|1234567890|test_nonce|order123|100.00";
        var secret = "test_secret_key";
        var signature = _hmacService.ComputeSignature(signatureBase1, secret);

        // Act
        var isValid = _hmacService.VerifySignature(signatureBase2, signature, secret);

        // Assert
        isValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("", "secret")]
    [InlineData("signature_base", "")]
    [InlineData("", "")]
    public void ComputeSignature_WithEmptyInput_HandlesGracefully(string signatureBase, string secret)
    {
        // Act & Assert
        var signature = _hmacService.ComputeSignature(signatureBase, secret);
        signature.Should().NotBeNull();
    }
}
