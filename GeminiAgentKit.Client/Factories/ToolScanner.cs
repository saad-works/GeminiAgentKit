using GeminiAgentKit.Client.Utilities;
using GeminiAgentKit.Types.Attributes;
using GeminiAgentKit.Types.Schema;
using GeminiAgentKit.Types.Tools;
using System.Reflection;

namespace GeminiAgentKit.Client.Factories;

public static class ToolScanner
{
    public static List<FunctionDeclaration> Scan<T>() where T : new()
        => Scan(typeof(T), new T());

    public static List<FunctionDeclaration> Scan(object instance)
        => Scan(instance.GetType(), instance);

    private static List<FunctionDeclaration> Scan(Type type, object? instance)
    {
        return type
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Select(m => (Method: m, Attr: m.GetCustomAttribute<GeminiFunctionAttribute>()))
            .Where(x => x.Attr is not null)
            .Select(x =>
            {
                var method = x.Method;
                var attr = x.Attr!;
                var parameters = method.GetParameters();
                var target = method.IsStatic ? null : instance;
                var name = attr.Name ?? method.Name;

                var declaration = new FunctionDeclaration
                {
                    Name = NameSanitizer.Sanitize(name),
                    Description = attr.Description,
                    Parameters = GeminiSchemaBuilder.Build(parameters)
                };

                declaration.SetHandler(args => method.Invoke(target, args), parameters);
                return declaration;
            })
            .ToList();
    }
}
