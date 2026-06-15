using System.Text.Json.Nodes;

namespace GeminiAgentKit.Types.Tools;

/// <summary>
/// Represents a function call issued by the model in a response candidate.
/// The agent loop matches <see cref="Name"/> against registered <see cref="FunctionDeclaration"/> entries
/// and invokes the corresponding handler.
/// </summary>
public class FunctionCall
{
    /// <summary>The name of the function the model wants to invoke.</summary>
    public required string Name { get; set; }

    /// <summary>Arguments supplied by the model, serialized as a JSON object keyed by parameter name.</summary>
    public JsonNode? Args { get; set; }
}
