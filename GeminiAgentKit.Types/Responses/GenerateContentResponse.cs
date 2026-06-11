using GeminiAgentKit.Types.Tools;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GeminiAgentKit.Types.Responses;

public class GenerateContentResponse
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public List<Candidate> Candidates { get; set; } = [];
    public UsageMetadata? UsageMetadata { get; set; }
    public string? ModelVersion { get; set; }
    public string? ResponseId { get; set; }
    public string? Text => Candidates.FirstOrDefault()?.Content?.Parts?.LastOrDefault(p => p.Text != null)?.Text;
    public FunctionCall? FunctionCall => Candidates.FirstOrDefault()?.Content?.Parts?.FirstOrDefault(p => p.FunctionCall != null)?.FunctionCall;

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

public class GenerateContentResponse<T> : GenerateContentResponse
{
    public GenerateContentResponse()
    {
    }

    public GenerateContentResponse(GenerateContentResponse source)
    {
        Candidates = source.Candidates;
        UsageMetadata = source.UsageMetadata;
        ModelVersion = source.ModelVersion;
        ResponseId = source.ResponseId;
    }
    public T? Result { get; set; }
}