using GeminiAgentKit.Types.Tools;

namespace GeminiAgentKit.Types.Contents;

/// <summary>
/// A single part within a <see cref="Content"/> message.
/// Exactly one of the properties is expected to be non-null in a given part,
/// depending on whether it carries text, a function call, or a function response.
/// </summary>
public class Part
{
    /// <summary>Plain text content of this part.</summary>
    public string? Text { get; set; }

    /// <summary>Opaque signature used by thinking models to carry extended reasoning state across turns.</summary>
    public string? ThoughtSignature { get; set; }

    /// <summary>A function call the model wants to invoke. Set by the model in a response candidate.</summary>
    public FunctionCall? FunctionCall { get; set; }

    /// <summary>The result of a tool invocation sent back to the model. Set by the agent loop after executing a tool.</summary>
    public FunctionResponse? FunctionResponse { get; set; }
}
