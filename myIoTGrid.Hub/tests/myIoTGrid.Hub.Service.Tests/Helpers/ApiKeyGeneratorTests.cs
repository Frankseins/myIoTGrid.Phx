using FluentAssertions;
using myIoTGrid.Hub.Service.Helpers;

namespace myIoTGrid.Hub.Service.Tests.Helpers;

/// <summary>
/// Tests for ApiKeyGenerator helper class.
/// </summary>
public class ApiKeyGeneratorTests
{
    #region GenerateApiKey Tests

    [Fact]
    public void GenerateApiKey_ReturnsKeyWithCorrectPrefix()
    {
        // Act
        var apiKey = ApiKeyGenerator.GenerateApiKey();

        // Assert
        apiKey.Should().StartWith("mig_key_");
    }

    [Fact]
    public void GenerateApiKey_ReturnsKeyWithCorrectLength()
    {
        // Act
        var apiKey = ApiKeyGenerator.GenerateApiKey();

        // Assert
        // mig_key_ (8 chars) + 32 random chars = 40 total
        apiKey.Should().HaveLength(40);
    }

    [Fact]
    public void GenerateApiKey_ReturnsUniqueKeys()
    {
        // Act
        var keys = Enumerable.Range(0, 100)
            .Select(_ => ApiKeyGenerator.GenerateApiKey())
            .ToList();

        // Assert
        keys.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void GenerateApiKey_ContainsOnlyAllowedCharacters()
    {
        // Arrange
        const string allowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        // Act
        var apiKey = ApiKeyGenerator.GenerateApiKey();
        var randomPart = apiKey["mig_key_".Length..];

        // Assert
        randomPart.ToCharArray().Should().OnlyContain(c => allowedChars.Contains(c));
    }

    [Fact]
    public void GenerateApiKey_ProducesRandomDistribution()
    {
        // Act
        var keys = Enumerable.Range(0, 1000)
            .Select(_ => ApiKeyGenerator.GenerateApiKey())
            .ToList();

        // Extract random parts
        var randomParts = keys.Select(k => k["mig_key_".Length..]).ToList();

        // Check that we have variety in characters at each position
        for (int i = 0; i < 32; i++)
        {
            var charsAtPosition = randomParts.Select(r => r[i]).Distinct().Count();
            // Should have reasonable variety (at least 10 different chars)
            charsAtPosition.Should().BeGreaterThan(10);
        }
    }

    #endregion

    #region HashApiKey Tests

    [Fact]
    public void HashApiKey_ReturnsDeterministicHash()
    {
        // Arrange
        var apiKey = "mig_key_abcdefghijklmnopqrstuvwxyz123456";

        // Act
        var hash1 = ApiKeyGenerator.HashApiKey(apiKey);
        var hash2 = ApiKeyGenerator.HashApiKey(apiKey);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void HashApiKey_ReturnsCorrectLength()
    {
        // Arrange
        var apiKey = ApiKeyGenerator.GenerateApiKey();

        // Act
        var hash = ApiKeyGenerator.HashApiKey(apiKey);

        // Assert
        // SHA256 produces 64 hex characters
        hash.Should().HaveLength(64);
    }

    [Fact]
    public void HashApiKey_ReturnsLowercaseHex()
    {
        // Arrange
        var apiKey = ApiKeyGenerator.GenerateApiKey();

        // Act
        var hash = ApiKeyGenerator.HashApiKey(apiKey);

        // Assert
        hash.Should().MatchRegex("^[a-f0-9]+$");
    }

    [Fact]
    public void HashApiKey_DifferentKeysProduceDifferentHashes()
    {
        // Arrange
        var key1 = "mig_key_abcdefghijklmnopqrstuvwxyz123456";
        var key2 = "mig_key_abcdefghijklmnopqrstuvwxyz123457"; // One char different

        // Act
        var hash1 = ApiKeyGenerator.HashApiKey(key1);
        var hash2 = ApiKeyGenerator.HashApiKey(key2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void HashApiKey_IsIrreversible()
    {
        // Arrange
        var apiKey = ApiKeyGenerator.GenerateApiKey();

        // Act
        var hash = ApiKeyGenerator.HashApiKey(apiKey);

        // Assert
        // The hash should not contain any part of the original key
        var randomPart = apiKey["mig_key_".Length..];
        hash.Should().NotContain(randomPart);
    }

    #endregion

    #region IsValidFormat Tests

    [Fact]
    public void IsValidFormat_WithValidKey_ReturnsTrue()
    {
        // Arrange
        var apiKey = ApiKeyGenerator.GenerateApiKey();

        // Act
        var result = ApiKeyGenerator.IsValidFormat(apiKey);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidFormat_WithNullKey_ReturnsFalse()
    {
        // Act
        var result = ApiKeyGenerator.IsValidFormat(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidFormat_WithEmptyKey_ReturnsFalse()
    {
        // Act
        var result = ApiKeyGenerator.IsValidFormat("");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidFormat_WithWhitespaceKey_ReturnsFalse()
    {
        // Act
        var result = ApiKeyGenerator.IsValidFormat("   ");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidFormat_WithWrongPrefix_ReturnsFalse()
    {
        // Arrange
        var apiKey = "pk_live_abcdefghijklmnopqrstuvwxyz123456";

        // Act
        var result = ApiKeyGenerator.IsValidFormat(apiKey);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidFormat_WithShortRandomPart_ReturnsFalse()
    {
        // Arrange
        var apiKey = "mig_key_tooshort";

        // Act
        var result = ApiKeyGenerator.IsValidFormat(apiKey);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidFormat_WithLongRandomPart_ReturnsFalse()
    {
        // Arrange
        var apiKey = "mig_key_abcdefghijklmnopqrstuvwxyz123456toolong";

        // Act
        var result = ApiKeyGenerator.IsValidFormat(apiKey);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidFormat_WithInvalidCharacters_ReturnsFalse()
    {
        // Arrange
        var apiKey = "mig_key_abcdefghijklmnopqrstuvwxyz12-_!#"; // 32 chars but with invalid chars

        // Act
        var result = ApiKeyGenerator.IsValidFormat(apiKey);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidFormat_WithValidManualKey_ReturnsTrue()
    {
        // Arrange
        var apiKey = "mig_key_abcdefghijklmnopqrstuvwxyz123456"; // Exactly 32 chars after prefix

        // Act
        var result = ApiKeyGenerator.IsValidFormat(apiKey);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region ValidateApiKey Tests

    [Fact]
    public void ValidateApiKey_WithCorrectKeyAndHash_ReturnsTrue()
    {
        // Arrange
        var apiKey = ApiKeyGenerator.GenerateApiKey();
        var hash = ApiKeyGenerator.HashApiKey(apiKey);

        // Act
        var result = ApiKeyGenerator.ValidateApiKey(apiKey, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateApiKey_WithWrongKey_ReturnsFalse()
    {
        // Arrange
        var apiKey1 = ApiKeyGenerator.GenerateApiKey();
        var apiKey2 = ApiKeyGenerator.GenerateApiKey();
        var hash = ApiKeyGenerator.HashApiKey(apiKey1);

        // Act
        var result = ApiKeyGenerator.ValidateApiKey(apiKey2, hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateApiKey_WithInvalidFormatKey_ReturnsFalse()
    {
        // Arrange
        var invalidKey = "invalid_key";
        var someHash = "somehash";

        // Act
        var result = ApiKeyGenerator.ValidateApiKey(invalidKey, someHash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateApiKey_IsCaseInsensitiveForHash()
    {
        // Arrange
        var apiKey = ApiKeyGenerator.GenerateApiKey();
        var hashLower = ApiKeyGenerator.HashApiKey(apiKey);
        var hashUpper = hashLower.ToUpperInvariant();

        // Act
        var result = ApiKeyGenerator.ValidateApiKey(apiKey, hashUpper);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateApiKey_WithNullKey_ReturnsFalse()
    {
        // Arrange
        var hash = "somehash";

        // Act
        var result = ApiKeyGenerator.ValidateApiKey(null!, hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateApiKey_WithEmptyHash_ReturnsFalse()
    {
        // Arrange
        var apiKey = ApiKeyGenerator.GenerateApiKey();

        // Act
        var result = ApiKeyGenerator.ValidateApiKey(apiKey, "");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void FullWorkflow_GenerateHashAndValidate_Works()
    {
        // Arrange & Act
        var apiKey = ApiKeyGenerator.GenerateApiKey();
        var hash = ApiKeyGenerator.HashApiKey(apiKey);
        var isValid = ApiKeyGenerator.ValidateApiKey(apiKey, hash);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void FullWorkflow_MultipleKeys_AllValidateCorrectly()
    {
        // Arrange
        var keyHashPairs = Enumerable.Range(0, 10)
            .Select(_ =>
            {
                var key = ApiKeyGenerator.GenerateApiKey();
                var hash = ApiKeyGenerator.HashApiKey(key);
                return (key, hash);
            })
            .ToList();

        // Act & Assert
        foreach (var (key, hash) in keyHashPairs)
        {
            ApiKeyGenerator.ValidateApiKey(key, hash).Should().BeTrue();

            // Other keys should not validate against this hash
            foreach (var (otherKey, _) in keyHashPairs.Where(p => p.key != key))
            {
                ApiKeyGenerator.ValidateApiKey(otherKey, hash).Should().BeFalse();
            }
        }
    }

    #endregion
}
