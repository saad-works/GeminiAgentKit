using GeminiAgentKit.Types.Tools;

namespace GeminiAgentKit.Types.Contents;

public class Part
{
    public string? Text { get; set; }
    public string? ThoughtSignature { get; set; }
    public FunctionCall? FunctionCall { get; set; }
    public FunctionResponse? FunctionResponse { get; set; }
}
