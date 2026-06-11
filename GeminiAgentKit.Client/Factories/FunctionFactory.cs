using GeminiAgentKit.Client.Utilities;
using GeminiAgentKit.Types.Schema;
using GeminiAgentKit.Types.Tools;

namespace GeminiAgentKit.Client.Factories;

public static class FunctionFactory
{
    public static FunctionDeclaration Create(string name, string description, Delegate handler)
    {
        var parameters = handler.Method.GetParameters();

        var declaration = new FunctionDeclaration
        {
            Name        = NameSanitizer.Sanitize(name),
            Description = description,
            Parameters  = GeminiSchemaBuilder.Build(parameters)
        };

        declaration.SetHandler(handler, parameters);

        return declaration;
    }
}
