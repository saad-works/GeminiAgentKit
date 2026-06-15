namespace GeminiAgentKit.Types.Chat;

/// <summary>A single message in a conversation, consisting of text and the role of its author.</summary>
/// <param name="Text">The message content.</param>
/// <param name="Role">The author role — see <see cref="GeminiAgentKit.Types.System.Role"/> for valid values.</param>
public record Message(string Text, string Role);
