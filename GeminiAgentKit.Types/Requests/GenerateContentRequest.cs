using GeminiAgentKit.Types.Configurations;
using GeminiAgentKit.Types.Contents;
using GeminiAgentKit.Types.Tools;

namespace GeminiAgentKit.Types.Requests;

/// <summary>
/// The request body sent to the Gemini <c>generateContent</c> API endpoint.
/// Built and managed internally by <c>RequestBuilder</c> and the agent loop — consumers interact
/// with <c>ChatHistory</c> rather than this type directly.
/// </summary>
public class GenerateContentRequest
{
    /// <summary>The ordered conversation turns, including user messages, model responses, and function call/response pairs.</summary>
    public List<Content> Contents { get; set; } = new();

    /// <summary>A system-level instruction prepended to every request to set the model's persona or constraints.</summary>
    public Content? SystemInstruction { get; set; }

    /// <summary>Controls the format of the model's response, such as requiring JSON output conforming to a schema.</summary>
    public GenerationConfig? GenerationConfig { get; set; }

    /// <summary>The tool declarations advertised to the model, defining which functions it may call.</summary>
    public List<Tool> Tools { get; set; } = new();
}
