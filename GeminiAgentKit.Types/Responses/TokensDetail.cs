namespace GeminiAgentKit.Types.Responses;

/// <summary>Token count breakdown for a specific modality within a <see cref="UsageMetadata"/> entry.</summary>
public class TokensDetail
{
    /// <summary>The modality these tokens belong to, e.g. <c>TEXT</c> or <c>IMAGE</c>.</summary>
    public string? Modality { get; set; }

    /// <summary>Number of tokens for this modality.</summary>
    public int TokenCount { get; set; }
}
