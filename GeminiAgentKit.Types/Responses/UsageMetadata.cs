namespace GeminiAgentKit.Types.Responses;

public class UsageMetadata
{
    public int PromptTokenCount { get; set; }
    public int CandidatesTokenCount { get; set; }
    public int TotalTokenCount { get; set; }
    public List<TokensDetail> PromptTokensDetails { get; set; } = [];
    public int ThoughtsTokenCount { get; set; }
    public string? ServiceTier { get; set; }
}
