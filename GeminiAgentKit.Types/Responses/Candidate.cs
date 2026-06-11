using GeminiAgentKit.Types.Contents;

namespace GeminiAgentKit.Types.Responses;

public class Candidate
{
    public Content? Content { get; set; }
    public string? FinishReason { get; set; }
    public int Index { get; set; }
}
