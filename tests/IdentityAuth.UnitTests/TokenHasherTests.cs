using IdentityAuth.Infrastructure.Security;

namespace IdentityAuth.UnitTests;

public class TokenHasherTests
{
    private readonly TokenHasher _hasher = new();

    [Fact]
    public void HashToken_ReturnsNonEmptyString()
    {
        var hash = _hasher.HashToken("test-token");

        Assert.False(string.IsNullOrEmpty(hash));
    }

    [Fact]
    public void HashToken_SameInput_ReturnsSameHash()
    {
        var hash1 = _hasher.HashToken("test-token");
        var hash2 = _hasher.HashToken("test-token");

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void HashToken_DifferentInput_ReturnsDifferentHash()
    {
        var hash1 = _hasher.HashToken("token-1");
        var hash2 = _hasher.HashToken("token-2");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void HashToken_NullInput_ThrowsException()
    {
        Assert.Throws<ArgumentNullException>(() => _hasher.HashToken(null!));
    }

    [Fact]
    public void HashToken_EmptyInput_ThrowsException()
    {
        Assert.Throws<ArgumentNullException>(() => _hasher.HashToken(""));
    }

    [Fact]
    public void VerifyToken_CorrectToken_ReturnsTrue()
    {
        var token = "my-secret-token";
        var hash = _hasher.HashToken(token);

        var result = _hasher.VerifyToken(token, hash);

        Assert.True(result);
    }

    [Fact]
    public void VerifyToken_WrongToken_ReturnsFalse()
    {
        var hash = _hasher.HashToken("correct-token");

        var result = _hasher.VerifyToken("wrong-token", hash);

        Assert.False(result);
    }

    [Fact]
    public void VerifyToken_EmptyToken_ReturnsFalse()
    {
        var hash = _hasher.HashToken("some-token");

        var result = _hasher.VerifyToken("", hash);

        Assert.False(result);
    }

    [Fact]
    public void VerifyToken_NullToken_ReturnsFalse()
    {
        var hash = _hasher.HashToken("some-token");

        var result = _hasher.VerifyToken(null!, hash);

        Assert.False(result);
    }

    [Fact]
    public void VerifyToken_NullHash_ReturnsFalse()
    {
        var result = _hasher.VerifyToken("some-token", null!);

        Assert.False(result);
    }
}
