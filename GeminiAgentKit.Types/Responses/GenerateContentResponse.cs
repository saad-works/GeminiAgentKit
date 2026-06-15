using GeminiAgentKit.Types.Tools;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GeminiAgentKit.Types.Responses;

/// <summary>
/// The response returned by the Gemini <c>generateContent</c> API endpoint.
/// Provides convenience accessors for the model's text output and any function call it issued.
/// </summary>
public class GenerateContentResponse
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>The list of response candidates returned by the model. Typically contains a single entry.</summary>
    public List<Candidate> Candidates { get; set; } = [];

    /// <summary>Token usage statistics for the request and response.</summary>
    public UsageMetadata? UsageMetadata { get; set; }

    /// <summary>The model version that generated the response.</summary>
    public string? ModelVersion { get; set; }

    /// <summary>A server-assigned identifier for this response.</summary>
    public string? ResponseId { get; set; }

    /// <summary>
    /// The final text content from the first candidate's last text part,
    /// or <see langword="null"/> if the response contains no text (e.g. it is a function call).
    /// </summary>
    public string? Text => Candidates.FirstOrDefault()?.Content?.Parts?.LastOrDefault(p => p.Text != null)?.Text;

    /// <summary>
    /// The function call issued by the model, if the response is a tool invocation rather than a text reply.
    /// <see langword="null"/> when the model returned a text response.
    /// </summary>
    public FunctionCall? FunctionCall => Candidates.FirstOrDefault()?.Content?.Parts?.FirstOrDefault(p => p.FunctionCall != null)?.FunctionCall;

    /// <summary>
    /// Deserializes the text response as JSON into a typed <see cref="GenerateContentResponse{T}"/>.
    /// Strips markdown code fences before deserializing.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the model's JSON output into.</typeparam>
    public GenerateContentResponse<T> ToTyped<T>()
    {
        var json = StripMarkdownFences(Text);

        return new GenerateContentResponse<T>(this)
        {
            Result = string.IsNullOrWhiteSpace(json)
                ? default
                : JsonSerializer.Deserialize<T>(json, _jsonOptions)
        };
    }

    private static string? StripMarkdownFences(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;

        var full = Regex.Match(text, @"```(?:json)?\s*([\s\S]*?)```");
        if (full.Success) return full.Groups[1].Value.Trim();

        var trailing = Regex.Match(text, @"^([\s\S]*?)```\s*$");
        if (trailing.Success) return trailing.Groups[1].Value.Trim();

        return text.Trim();
    }
}

/// <summary>
/// A typed variant of <see cref="GenerateContentResponse"/> that exposes the model's JSON output
/// deserialized into <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type the model's JSON response is deserialized into.</typeparam>
public class GenerateContentResponse<T> : GenerateContentResponse
{
    /// <summary>Initializes an empty typed response.</summary>
    public GenerateContentResponse()
    {
    }

    /// <summary>Initializes a typed response by copying base properties from an existing <see cref="GenerateContentResponse"/>.</summary>
    /// <param name="source">The untyped response to copy metadata from.</param>
    public GenerateContentResponse(GenerateContentResponse source)
    {
        Candidates = source.Candidates;
        UsageMetadata = source.UsageMetadata;
        ModelVersion = source.ModelVersion;
        ResponseId = source.ResponseId;
    }

    /// <summary>The deserialized result of the model's JSON response, or <see langword="default"/> if deserialization produced no value.</summary>
    public T? Result { get; set; }
}
