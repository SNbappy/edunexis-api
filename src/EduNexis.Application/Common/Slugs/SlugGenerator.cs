using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using EduNexis.Domain.Interfaces.Repositories;

namespace EduNexis.Application.Common.Slugs;

/// <summary>
/// Converts arbitrary names into URL-safe slugs (lowercase, ASCII, hyphenated)
/// and ensures uniqueness against the database. Reserved slugs are blocked.
/// </summary>
public static class SlugGenerator
{
    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin", "api", "login", "register", "dashboard", "courses", "profile",
        "settings", "faculty", "about", "home", "root", "public", "www",
        "404", "logout", "signup", "signin", "edit", "new", "create", "delete",
    };

    /// <summary>
    /// Format-only validation. Does NOT check uniqueness. Returns null if valid,
    /// or a user-facing error message if invalid.
    /// </summary>
    public static string? Validate(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return "Slug is required.";
        if (slug.Length < 3 || slug.Length > 30)
            return "Slug must be between 3 and 30 characters.";
        if (!Regex.IsMatch(slug, "^[a-z0-9-]+$"))
            return "Slug can only contain lowercase letters, numbers, and hyphens.";
        if (slug.StartsWith("-") || slug.EndsWith("-"))
            return "Slug cannot start or end with a hyphen.";
        if (slug.Contains("--"))
            return "Slug cannot contain consecutive hyphens.";
        if (Reserved.Contains(slug))
            return "This slug is reserved.";
        return null;
    }

    /// <summary>
    /// Convert a full name to a base slug. "Md. Nowsin Amin Sheikh" → "nowsin-amin-sheikh".
    /// Strips honorifics (Md, Dr, etc.) and trailing tokens that look noise.
    /// </summary>
    public static string Slugify(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "user";

        // Normalize unicode to ASCII (handles é, ñ, etc.)
        var normalized = fullName.Normalize(NormalizationForm.FormD);
        var ascii = new StringBuilder();
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                ascii.Append(ch);
        }
        var s = ascii.ToString().ToLowerInvariant();

        // Strip honorifics anywhere in the string
        var honorifics = new[] { "md.", "md", "mr.", "mr", "mrs.", "mrs", "ms.", "ms", "dr.", "dr", "prof.", "prof" };
        var tokens = Regex.Split(s, @"[\s.,]+")
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Where(t => !honorifics.Contains(t))
            .ToArray();

        var joined = string.Join("-", tokens);
        joined = Regex.Replace(joined, "[^a-z0-9-]", "");
        joined = Regex.Replace(joined, "-+", "-").Trim('-');

        if (string.IsNullOrEmpty(joined)) joined = "user";
        if (joined.Length > 30) joined = joined.Substring(0, 30).TrimEnd('-');
        return joined;
    }

    /// <summary>
    /// Generates a slug from the name and resolves collisions by appending -2, -3, etc.
    /// Excludes the calling user's existing slug from the collision check.
    /// </summary>
    public static async Task<string> GenerateUniqueAsync(
        string fullName, Guid userId, IUserProfileRepository repo, CancellationToken ct = default)
    {
        var baseSlug = Slugify(fullName);
        if (Reserved.Contains(baseSlug)) baseSlug = baseSlug + "-edu";

        var candidate = baseSlug;
        var suffix = 2;
        while (await repo.IsSlugTakenAsync(candidate, userId, ct))
        {
            candidate = $"{baseSlug}-{suffix}";
            if (candidate.Length > 30) candidate = candidate.Substring(0, 30).TrimEnd('-');
            suffix++;
            if (suffix > 100) break; // safety valve
        }
        return candidate;
    }
}