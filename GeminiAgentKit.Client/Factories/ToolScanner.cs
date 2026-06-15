using GeminiAgentKit.Client.Utilities;
using GeminiAgentKit.Types.Attributes;
using GeminiAgentKit.Types.Schema;
using GeminiAgentKit.Types.Tools;
using System.Reflection;

namespace GeminiAgentKit.Client.Factories;

/// <summary>
/// Reflects over a type or instance to discover methods decorated with <see cref="GeminiFunctionAttribute"/>
/// and converts them into <see cref="FunctionDeclaration"/> entries ready for registration with <c>GeminiClient</c>.
/// </summary>
public static class ToolScanner
{
    /// <summary>
    /// Instantiates <typeparamref name="T"/> and scans all its public methods for <see cref="GeminiFunctionAttribute"/>.
    /// Supports both static and instance methods.
    /// </summary>
    /// <typeparam name="T">The tool class to scan. Must have a parameterless constructor.</typeparam>
    public static List<FunctionDeclaration> Scan<T>() where T : new()
        => Scan(typeof(T), new T());

    /// <summary>
    /// Scans all public methods on <paramref name="instance"/> for <see cref="GeminiFunctionAttribute"/>.
    /// Use this overload when the tool class requires constructor arguments.
    /// </summary>
    /// <param name="instance">The tool instance to scan.</param>
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
