namespace GeminiAgentKit.Types.Contents;

/// <summary>
/// A single message in the conversation, consisting of a role and one or more content parts.
/// Used for user turns, model turns, and function call/response exchanges.
/// </summary>
public class Content
{
    /// <summary>The author of this message — see <see cref="GeminiAgentKit.Types.System.Role"/> for valid values.</summary>
    public required string Role { get; set; }

    /// <summary>One or more parts making up the content of this message.</summary>
    public required List<Part> Parts { get; set; }
}
