using System.Text.RegularExpressions;

namespace GeminiAgentKit.Client.Utilities;

/// <summary>
/// Normalizes tool names to conform to the Gemini API's function name requirements:
/// only letters, digits, underscores, hyphens, and dots are allowed, and the name must start with a letter or underscore.
/// </summary>
public static class NameSanitizer
{
    /// <summary>
    /// Sanitizes <paramref name="name"/> by trimming whitespace, replacing spaces with underscores,
    /// stripping disallowed characters, and prepending an underscore if the name starts with a digit.
    /// </summary>
    /// <param name="name">The raw tool name to sanitize.</param>
    public static string Sanitize(string name)
    {
        name = name.Trim();
        name = Regex.Replace(name, @"\s+", "_");
        name = Regex.Replace(name, @"[^a-zA-Z0-9_.\-:]", "");
        if (name.Length > 0 && !char.IsLetter(name[0]) && name[0] != '_')
            name = "_" + name;
        return name;
    }
}
