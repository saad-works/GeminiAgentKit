using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace GeminiAgentKit.Types.Tools;

/// <summary>
/// Describes a callable tool that the Gemini model can invoke during the agent loop.
/// Holds the function name, description, JSON Schema for its parameters, and an internal
/// invoker delegate wired up by <c>FunctionFactory</c> or <c>ToolScanner</c>.
/// </summary>
public class FunctionDeclaration
{
    private static readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    private Func<object?[], object?>? _invoker;
    private ParameterInfo[]? _params;

    /// <summary>The function name sent to the model. Must be unique within a request.</summary>
    public required string Name { get; set; }

    /// <summary>Plain-English description of what the function does, used by the model to decide when to call it.</summary>
    public required string Description { get; set; }

    /// <summary>JSON Schema object describing the function's parameters, or <see langword="null"/> if the function takes no parameters.</summary>
    public JsonNode? Parameters { get; set; }

    /// <summary><see langword="true"/> if an invoker delegate has been registered via <c>SetHandler</c>.</summary>
    [JsonIgnore]
    public bool HasHandler => _invoker is not null;

    /// <summary>
    /// Invokes the underlying handler with arguments deserialized from the model's <see cref="FunctionCall"/>.
    /// Returns <see langword="null"/> if no handler is registered.
    /// </summary>
    /// <param name="args">Raw JSON arguments supplied by the model, keyed by parameter name.</param>
    public object? Invoke(JsonNode? args)
    {
        if (_invoker is null) return null;

        if (_params is null || _params.Length == 0)
            return _invoker([]);

        var invokeArgs = _params
            .Select(p => args?[p.Name!] is JsonNode node
                ? JsonSerializer.Deserialize(node, p.ParameterType, _options)
                : p.HasDefaultValue ? p.DefaultValue : null)
            .ToArray();

        return _invoker(invokeArgs);
    }

    internal void SetHandler(Func<object?[], object?> invoker, ParameterInfo[] parameters)
    {
        _invoker = invoker;
        _params  = parameters;
    }
}
