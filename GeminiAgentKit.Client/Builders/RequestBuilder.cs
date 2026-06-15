using GeminiAgentKit.Types.Chat;
using GeminiAgentKit.Types.Configurations;
using GeminiAgentKit.Types.Constants;
using GeminiAgentKit.Types.Contents;
using GeminiAgentKit.Types.Requests;
using GeminiAgentKit.Types.Schema;
using GeminiAgentKit.Types.System;
using GeminiAgentKit.Types.Tools;

namespace GeminiAgentKit.Client.Builders;

/// <summary>
/// Assembles a <see cref="GenerateContentRequest"/> from instructions, registered tools,
/// and an optional <see cref="ChatHistory"/> to replay prior conversation turns.
/// </summary>
public static class RequestBuilder
{
    /// <summary>
    /// Builds a <see cref="GenerateContentRequest"/> for a text response.
    /// If <paramref name="history"/> is provided its messages are replayed into <c>Contents</c>
    /// before the new <paramref name="instructions"/> are appended.
    /// </summary>
    /// <param name="instructions">The user message for the current turn.</param>
    /// <param name="tools">Tool declarations to advertise to the model.</param>
    /// <param name="history">Prior conversation turns to include as context, or <see langword="null"/> for a fresh conversation.</param>
    public static GenerateContentRequest Build(
        string instructions,
        List<FunctionDeclaration> tools,
        ChatHistory? history = null)
    {
        var request = new GenerateContentRequest();
        request.SystemInstruction = new Content { Parts = [new Part { Text = SystemPrompt.Default }], Role = Role.Model };

        if (tools.Count > 0)
            request.Tools.Add(new Tool { FunctionDeclarations = tools });

        if (history != null)
            foreach (var message in history.Messages)
                request.Contents.Add(new Content { Parts = [new Part { Text = message.Text }], Role = message.Role });

        request.Contents.Add(new Content { Parts = [new Part { Text = instructions }], Role = Role.User });

        return request;
    }

    /// <summary>
    /// Builds a <see cref="GenerateContentRequest"/> for a structured JSON response of type <typeparamref name="T"/>.
    /// Adds a <see cref="GenerationConfig"/> constraining the response to a JSON Schema derived from <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type whose JSON Schema is used to constrain the model's response.</typeparam>
    /// <param name="instructions">The user message for the current turn.</param>
    /// <param name="tools">Tool declarations to advertise to the model.</param>
    /// <param name="history">Prior conversation turns to include as context, or <see langword="null"/> for a fresh conversation.</param>
    public static GenerateContentRequest Build<T>(
        string instructions,
        List<FunctionDeclaration> tools,
        ChatHistory? history = null)
    {
        var request = Build(instructions, tools, history);

        request.GenerationConfig = new GenerationConfig
        {
            ResponseMimeType = ResponseType.Json,
            ResponseSchema   = GeminiSchemaBuilder.Build<T>()
        };

        return request;
    }
}
