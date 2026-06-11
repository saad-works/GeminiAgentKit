using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace GeminiAgentKit.Types.Schema;

public static class GeminiSchemaBuilder
{
    public static JsonNode Build<T>() => BuildSchema(typeof(T));
    public static JsonNode Build(Type type) => BuildSchema(type);

    public static JsonNode? Build(ParameterInfo[] parameters)
    {
        if (parameters.Length == 0) return null;

        var obj        = new JsonObject { ["type"] = "object" };
        var properties = new JsonObject();
        var required   = new JsonArray();

        foreach (var param in parameters)
        {
            var name = param.Name!;
            properties[name] = BuildSchema(param.ParameterType);
            if (!param.HasDefaultValue && Nullable.GetUnderlyingType(param.ParameterType) == null)
                required.Add(name);
        }

        obj["properties"] = properties;
        if (required.Count > 0)
            obj["required"] = required;

        return obj;
    }

    private static JsonNode BuildSchema(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying != null)
            type = underlying;

        if (type == typeof(string))   return new JsonObject { ["type"] = "string" };
        if (type == typeof(bool))     return new JsonObject { ["type"] = "boolean" };
        if (type == typeof(int)   || type == typeof(long)  || type == typeof(short))
                                      return new JsonObject { ["type"] = "integer" };
        if (type == typeof(double) || type == typeof(float) || type == typeof(decimal))
                                      return new JsonObject { ["type"] = "number" };

        if (type.IsEnum)
        {
            var enumSchema = new JsonObject { ["type"] = "string" };
            var values = new JsonArray();
            foreach (var name in Enum.GetNames(type))
                values.Add(name);
            enumSchema["enum"] = values;
            return enumSchema;
        }

        var elementType = GetCollectionElementType(type);
        if (elementType != null)
        {
            return new JsonObject
            {
                ["type"]  = "array",
                ["items"] = BuildSchema(elementType)
            };
        }

        var obj        = new JsonObject { ["type"] = "object" };
        var properties = new JsonObject();
        var required   = new JsonArray();

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var jsonName = prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
                ?? char.ToLowerInvariant(prop.Name[0]) + prop.Name[1..];

            properties[jsonName] = BuildSchema(prop.PropertyType);

            if (!IsNullable(prop))
                required.Add(jsonName);
        }

        obj["properties"] = properties;
        if (required.Count > 0)
            obj["required"] = required;

        return obj;
    }

    private static Type? GetCollectionElementType(Type type)
    {
        if (type.IsArray)
            return type.GetElementType();

        foreach (var iface in type.GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return iface.GetGenericArguments()[0];
        }

        return null;
    }

    private static bool IsNullable(PropertyInfo prop)
    {
        if (Nullable.GetUnderlyingType(prop.PropertyType) != null)
            return true;

        var ctx = new NullabilityInfoContext();
        return ctx.Create(prop).WriteState is NullabilityState.Nullable;
    }
}
