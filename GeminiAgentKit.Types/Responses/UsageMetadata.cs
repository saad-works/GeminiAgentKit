namespace GeminiAgentKit.Types.Responses;

/// <summary>Token usage statistics for a <see cref="GenerateContentResponse"/>.</summary>
public class UsageMetadata
{
    /// <summary>Number of tokens in the input prompt, including conversation history and tool definitions.</summary>
    public int PromptTokenCount { get; set; }

    /// <summary>Number of tokens across all response candidates.</summary>
    public int CandidatesTokenCount { get; set; }

    /// <summary>Total tokens consumed by the request (prompt + candidates).</summary>
    public int TotalTokenCount { get; set; }

    /// <summary>Breakdown of prompt token counts by modality.</summary>
    public List<TokensDetail> PromptTokensDetails { get; set; } = [];

    /// <summary>Tokens used for extended thinking, if a thinking model was used.</summary>
    public int ThoughtsTokenCount { get; set; }

    /// <summary>The service tier used to fulfill the request, if applicable.</summary>
    public string? ServiceTier { get; set; }
}
