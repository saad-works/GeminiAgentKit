using GeminiAgentKit.Types.System;

namespace GeminiAgentKit.Types.Chat;

/// <summary>
/// Maintains the ordered list of messages exchanged in a conversation.
/// Pass the same <see cref="ChatHistory"/> instance across multiple calls to
/// <c>GenerateContentAsync</c> to preserve context between turns.
/// </summary>
public class ChatHistory
{
    /// <summary>All messages in the conversation, in chronological order.</summary>
    public List<Message> Messages { get; set; } = [];

    /// <summary>Appends a user message to the conversation history.</summary>
    /// <param name="content">The text of the user message.</param>
    public void AddUserMessage(string content)
    {
        Messages.Add(new Message(content, Role.User));
    }

    /// <summary>Appends a model message to the conversation history.</summary>
    /// <param name="content">The text of the model response.</param>
    public void AddModelMessage(string content)
    {
        Messages.Add(new Message(content, Role.Model));
    }
}
