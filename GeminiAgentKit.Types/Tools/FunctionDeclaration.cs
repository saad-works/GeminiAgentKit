using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace GeminiAgentKit.Types.Tools;

public class FunctionDeclaration
{
    private static readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    private Delegate? _handler;
    private ParameterInfo[]? _params;

    public required string Name { get; set; }
    public required string Description { get; set; }
    public JsonNode? Parameters { get; set; }

    [JsonIgnore]
    public bool HasHandler => _handler is not null;

    public object? Invoke(JsonNode? args)
    {
        if (_handler is null) return null;

        if (_params is null || _params.Length == 0)
            return _handler.DynamicInvoke();

        var invokeArgs = _params
            .Select(p => args?[p.Name!] is JsonNode node
                ? JsonSerializer.Deserialize(node, p.ParameterType, _options)
                : p.HasDefaultValue ? p.DefaultValue : null)
            .ToArray();

        return _handler.DynamicInvoke(invokeArgs);
    }

    internal void SetHandler(Delegate handler, ParameterInfo[] parameters)
    {
        _handler = handler;
        _params  = parameters;
    }
}
