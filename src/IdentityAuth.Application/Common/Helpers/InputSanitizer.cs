using System.Text.RegularExpressions;

namespace IdentityAuth.Application.Common.Helpers;

/// <summary>
/// Provides input sanitization to prevent XSS attacks when values
/// may be rendered in HTML contexts (e.g., emails, admin panels).
/// </summary>
public static class InputSanitizer
{
    private static readonly Regex HtmlTagPattern = new(@"<[^>]*>", RegexOptions.Compiled);

    /// <summary>
    /// Sanitizes a user-supplied string by removing HTML tags and encoding
    /// dangerous characters. Safe for storage and later HTML rendering.
    /// </summary>
    public static string Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Remove HTML/script tags entirely
        var sanitized = HtmlTagPattern.Replace(input, string.Empty);

        // Encode remaining HTML-special characters to prevent XSS
        sanitized = sanitized
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#x27;");

        return sanitized.Trim();
    }
}
