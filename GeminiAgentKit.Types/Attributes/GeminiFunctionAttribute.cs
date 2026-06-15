namespace GeminiAgentKit.Types.Attributes;

/// <summary>
/// Marks a public method as a callable tool that the Gemini model can invoke during the agent loop.
/// Apply this to any static or instance method whose signature can be reflected into a JSON Schema.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class GeminiFunctionAttribute : Attribute
{
    /// <summary>Plain-English description of what the tool does, sent to the model as part of the function declaration.</summary>
    public string Description { get; }

    /// <summary>
    /// Optional override for the tool name sent to the model.
    /// Defaults to the method name when <see langword="null"/>.
    /// </summary>
    public string? Name { get; init; }

    /// <param name="description">Plain-English description of what the tool does.</param>
    public GeminiFunctionAttribute(string description)
    {
        Description = description;
    }
}
