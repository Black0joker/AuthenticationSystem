namespace IdentityAuth.Application.Common.Helpers;

public static class EmailNormalizer
{
    /// <summary>
    /// Normalizes an email address for consistent comparison.
    /// Converts to uppercase invariant culture and trims whitespace.
    /// </summary>
    public static string Normalize(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return string.Empty;

        return email.Trim().ToUpperInvariant();
    }
}
