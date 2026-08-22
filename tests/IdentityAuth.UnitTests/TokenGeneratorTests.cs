using IdentityAuth.Infrastructure.Security;

namespace IdentityAuth.UnitTests;

public class TokenGeneratorTests
{
    private readonly TokenGenerator _generator = new();

    [Fact]
    public void GenerateToken_ReturnsNonEmptyString()
    {
        var token = _generator.GenerateToken();

        Assert.False(string.IsNullOrEmpty(token));
    }

    [Fact]
    public void GenerateToken_ReturnsUniqueTokens()
    {
        var token1 = _generator.GenerateToken();
        var token2 = _generator.GenerateToken();

        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void GenerateToken_IsUrlSafe()
    {
        // Generate multiple tokens to increase chance of catching non-URL-safe chars
        for (int i = 0; i < 100; i++)
        {
            var token = _generator.GenerateToken();

            Assert.DoesNotContain("+", token);
            Assert.DoesNotContain("/", token);
            Assert.DoesNotContain("=", token);
        }
    }

    [Fact]
    public void GenerateToken_DefaultSize_ReturnsExpectedLength()
    {
        // 32 bytes -> ~43 chars in URL-safe Base64
        var token = _generator.GenerateToken(32);

        Assert.True(token.Length >= 40);
    }

    [Fact]
    public void GenerateToken_CustomSize_ReturnsToken()
    {
        var token = _generator.GenerateToken(64);

        Assert.False(string.IsNullOrEmpty(token));
        Assert.True(token.Length >= 80);
    }

    [Fact]
    public void GenerateToken_ZeroSize_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _generator.GenerateToken(0));
    }

    [Fact]
    public void GenerateToken_NegativeSize_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _generator.GenerateToken(-1));
    }
}
