namespace GeminiAgentKit.Types.Tools;

/// <summary>
/// A collection of <see cref="FunctionDeclaration"/> entries sent to the Gemini API to advertise
/// the tools available to the model during a request.
/// </summary>
public class Tool
{
    /// <summary>The function declarations that the model may choose to invoke.</summary>
    public List<FunctionDeclaration> FunctionDeclarations { get; set; } = [];
}
