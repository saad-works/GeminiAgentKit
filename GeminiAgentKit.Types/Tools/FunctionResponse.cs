using System.Text.Json.Nodes;

namespace GeminiAgentKit.Types.Tools;

/// <summary>
/// Wraps the result of a tool invocation to be sent back to the model as part of the next request.
/// The Gemini API requires the response to be keyed under the same name as the original <see cref="FunctionCall"/>.
/// </summary>
public class FunctionResponse
{
    /// <summary>The name of the function whose result this response carries. Must match the original <see cref="FunctionCall.Name"/>.</summary>
    public required string Name { get; set; }

    /// <summary>The serialized return value of the tool invocation, wrapped in a JSON object.</summary>
    public JsonNode? Response { get; set; }
}
