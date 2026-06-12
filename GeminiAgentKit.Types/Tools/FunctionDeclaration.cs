using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace GeminiAgentKit.Types.Tools;

public class FunctionDeclaration
{
    private static readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    private Func<object?[], object?>? _invoker;
    private ParameterInfo[]? _params;

    public required string Name { get; set; }
    public required string Description { get; set; }
    public JsonNode? Parameters { get; set; }

    [JsonIgnore]
    public bool HasHandler => _invoker is not null;

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
