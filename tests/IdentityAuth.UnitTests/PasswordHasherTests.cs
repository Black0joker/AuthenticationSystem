using IdentityAuth.Infrastructure.Security;

namespace IdentityAuth.UnitTests;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void HashPassword_ReturnsNonEmptyString()
    {
        var hash = _hasher.HashPassword("TestPassword1!");

        Assert.False(string.IsNullOrEmpty(hash));
    }

    [Fact]
    public void HashPassword_ReturnsThreePartFormat()
    {
        var hash = _hasher.HashPassword("TestPassword1!");
        var parts = hash.Split('.');

        Assert.Equal(3, parts.Length);
    }

    [Fact]
    public void HashPassword_SamePassword_DifferentHashes()
    {
        var hash1 = _hasher.HashPassword("TestPassword1!");
        var hash2 = _hasher.HashPassword("TestPassword1!");

        Assert.NotEqual(hash1, hash2); // Different salts
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        var password = "MySecureP@ss1";
        var hash = _hasher.HashPassword(password);

        var result = _hasher.VerifyPassword(password, hash);

        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_WrongPassword_ReturnsFalse()
    {
        var hash = _hasher.HashPassword("CorrectPassword1!");

        var result = _hasher.VerifyPassword("WrongPassword1!", hash);

        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_EmptyPassword_ReturnsFalse()
    {
        var hash = _hasher.HashPassword("TestPassword1!");

        var result = _hasher.VerifyPassword("", hash);

        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_InvalidHash_ReturnsFalse()
    {
        var result = _hasher.VerifyPassword("TestPassword1!", "invalid-hash");

        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_NullHash_ReturnsFalse()
    {
        var result = _hasher.VerifyPassword("TestPassword1!", null!);

        Assert.False(result);
    }

    [Fact]
    public void HashPassword_NullPassword_ThrowsException()
    {
        Assert.Throws<ArgumentNullException>(() => _hasher.HashPassword(null!));
    }
}
