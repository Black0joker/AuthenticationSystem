using IdentityAuth.Application.Common.Validators;

namespace IdentityAuth.UnitTests;

public class PasswordValidatorTests
{
    [Theory]
    [InlineData("StrongPass1!")]
    [InlineData("MyP@ssw0rd")]
    [InlineData("C0mplex!Pass")]
    public void Validate_ValidPassword_ReturnsTrue(string password)
    {
        var (isValid, errors) = PasswordValidator.Validate(password);

        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_EmptyPassword_ReturnsFalse()
    {
        var (isValid, errors) = PasswordValidator.Validate("");

        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("required"));
    }

    [Fact]
    public void Validate_NullPassword_ReturnsFalse()
    {
        var (isValid, errors) = PasswordValidator.Validate(null!);

        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("required"));
    }

    [Theory]
    [InlineData("Ab1!")]
    [InlineData("Short1!")]
    public void Validate_TooShortPassword_ReturnsFalse(string password)
    {
        var (isValid, errors) = PasswordValidator.Validate(password);

        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("at least 8"));
    }

    [Fact]
    public void Validate_NoUppercase_ReturnsFalse()
    {
        var (isValid, errors) = PasswordValidator.Validate("lowercase1!");

        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("uppercase"));
    }

    [Fact]
    public void Validate_NoLowercase_ReturnsFalse()
    {
        var (isValid, errors) = PasswordValidator.Validate("UPPERCASE1!");

        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("lowercase"));
    }

    [Fact]
    public void Validate_NoDigit_ReturnsFalse()
    {
        var (isValid, errors) = PasswordValidator.Validate("NoDigitHere!");

        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("digit"));
    }

    [Fact]
    public void Validate_NoSpecialCharacter_ReturnsFalse()
    {
        var (isValid, errors) = PasswordValidator.Validate("NoSpecial1x");

        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("special character"));
    }

    [Fact]
    public void Validate_MultipleErrors_ReturnsAll()
    {
        var (isValid, errors) = PasswordValidator.Validate("abc");

        Assert.False(isValid);
        Assert.True(errors.Count >= 3); // too short, no uppercase, no digit, no special
    }
}
