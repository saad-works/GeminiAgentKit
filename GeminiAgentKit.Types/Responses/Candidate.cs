using GeminiAgentKit.Types.Contents;

namespace GeminiAgentKit.Types.Responses;

/// <summary>A single response candidate returned by the model within a <see cref="GenerateContentResponse"/>.</summary>
public class Candidate
{
    /// <summary>The content of this candidate, containing the model's response parts.</summary>
    public Content? Content { get; set; }

    /// <summary>The reason the model stopped generating, e.g. <c>STOP</c> or <c>MAX_TOKENS</c>.</summary>
    public string? FinishReason { get; set; }

    /// <summary>Zero-based index of this candidate within the response's candidate list.</summary>
    public int Index { get; set; }
}
