namespace GeminiAgentKit.Types.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public sealed class GeminiFunctionAttribute : Attribute
{
    public string Description { get; }
    public string? Name { get; init; }

    public GeminiFunctionAttribute(string description)
    {
        Description = description;
    }
}
