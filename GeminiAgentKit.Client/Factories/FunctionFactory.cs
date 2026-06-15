using GeminiAgentKit.Client.Utilities;
using GeminiAgentKit.Types.Schema;
using GeminiAgentKit.Types.Tools;

namespace GeminiAgentKit.Client.Factories;

/// <summary>
/// Creates <see cref="FunctionDeclaration"/> instances from a delegate, reflecting on its parameter
/// types to generate the corresponding JSON Schema automatically.
/// </summary>
public static class FunctionFactory
{
    /// <summary>
    /// Creates a <see cref="FunctionDeclaration"/> from an explicit name, description, and handler delegate.
    /// Use this for inline tools that don't warrant a dedicated class; prefer <see cref="ToolScanner"/> and
    /// <c>[GeminiFunction]</c> for class-based tools.
    /// </summary>
    /// <param name="name">Tool name sent to the model. Sanitized automatically.</param>
    /// <param name="description">Plain-English description of what the tool does.</param>
    /// <param name="handler">Delegate implementing the tool. Its parameter types are reflected to build the JSON Schema.</param>
    public static FunctionDeclaration Create(string name, string description, Delegate handler)
    {
        var parameters = handler.Method.GetParameters();

        var declaration = new FunctionDeclaration
        {
            Name        = NameSanitizer.Sanitize(name),
            Description = description,
            Parameters  = GeminiSchemaBuilder.Build(parameters)
        };

        declaration.SetHandler(args => handler.DynamicInvoke(args), parameters);

        return declaration;
    }
}
