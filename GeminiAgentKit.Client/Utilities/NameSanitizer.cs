using System.Text.RegularExpressions;

namespace GeminiAgentKit.Client.Utilities;

public static class NameSanitizer
{
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
