using IdentityAuth.Application.Common.Helpers;

namespace IdentityAuth.UnitTests;

public class EmailNormalizerTests
{
    [Theory]
    [InlineData("user@example.com", "USER@EXAMPLE.COM")]
    [InlineData("User@Example.COM", "USER@EXAMPLE.COM")]
    [InlineData("  user@example.com  ", "USER@EXAMPLE.COM")]
    [InlineData("ADMIN@TEST.ORG", "ADMIN@TEST.ORG")]
    public void Normalize_ValidEmail_ReturnsUppercaseTrimmed(string input, string expected)
    {
        var result = EmailNormalizer.Normalize(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Normalize_EmptyOrNull_ReturnsEmpty(string? input)
    {
        var result = EmailNormalizer.Normalize(input!);

        Assert.Equal(string.Empty, result);
    }
}
