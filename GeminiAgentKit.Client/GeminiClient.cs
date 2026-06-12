using GeminiAgentKit.Client.Builders;
using GeminiAgentKit.Client.Exceptions;
using GeminiAgentKit.Client.Factories;
using GeminiAgentKit.Types.Config;
using GeminiAgentKit.Types.Constants;
using GeminiAgentKit.Types.Contents;
using GeminiAgentKit.Types.Requests;
using GeminiAgentKit.Types.Responses;
using GeminiAgentKit.Types.System;
using GeminiAgentKit.Types.Tools;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace GeminiAgentKit.Client;

public class GeminiClient
{
    private readonly List<FunctionDeclaration> _tools = [];
    private readonly GeminiConfiguration _configuration;
    private readonly HttpClient _httpClient = new();

    public GeminiClient() : this(GeminiConfigurationBuilder.Build()) { }

    public GeminiClient(GeminiConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void AddTools(List<FunctionDeclaration> tools) => _tools.AddRange(tools);
    public void AddTools<T>() where T : new() => _tools.AddRange(ToolScanner.Scan<T>());
    public void AddTools(object instance) => _tools.AddRange(ToolScanner.Scan(instance));

    public static FunctionDeclaration CreateFunction(string name, string description, Delegate handler)
        => FunctionFactory.Create(name, description, handler);

    public async Task<(GenerateContentRequest, GenerateContentResponse)> GenerateContentAsync(
        string instructions,
        GenerateContentRequest? request = null,
        string model = Model.Gemini35Flash,
        CancellationToken cancellationToken = default)
    {
        request = RequestBuilder.Build(instructions, _tools, request);
        var response = await RunAgentLoopAsync(request, model, cancellationToken);
        return (request, response);
    }

    public async Task<(GenerateContentRequest, GenerateContentResponse<T>)> GenerateContentAsync<T>(
        string instructions,
        GenerateContentRequest? request = null,
        string model = Model.Gemini35Flash,
        CancellationToken cancellationToken = default)
    {
        request = RequestBuilder.Build<T>(instructions, _tools, request);
        var response = await RunAgentLoopAsync(request, model, cancellationToken);
        return (request, response.ToTyped<T>());
    }

    private async Task<GenerateContentResponse> RunAgentLoopAsync(
        GenerateContentRequest request,
        string model,
        CancellationToken cancellationToken)
    {
        var response = await GenerateResponseAsync(request, model, cancellationToken);

        while (response.FunctionCall is { } call)
        {
            request.Contents.Add(response.Candidates.First().Content!);

            var declaration = _tools.FirstOrDefault(t => t.Name == call.Name);
            var rawResult   = declaration?.Invoke(call.Args);
            var result      = rawResult is Task task ? await UnwrapTaskAsync(task) : rawResult;

            request.Contents.Add(new Content
            {
                Role  = Role.User,
                Parts = [new Part
                {
                    FunctionResponse = new FunctionResponse
                    {
                        Name     = call.Name,
                        Response = JsonNode.Parse(JsonSerializer.Serialize(new { result }))
                    }
                }]
            });

            response = await GenerateResponseAsync(request, model, cancellationToken);
        }

        request.Contents.Add(response.Candidates.First().Content!);

        return response;
    }

    private static async Task<object?> UnwrapTaskAsync(Task task)
    {
        await task;
        var resultProperty = task.GetType().GetProperty("Result");
        return resultProperty?.GetValue(task);
    }

    private async Task<GenerateContentResponse> GenerateResponseAsync(
        GenerateContentRequest request,
        string model,
        CancellationToken cancellationToken)
    {
        var url = _configuration.BaseUrl.Replace(Placeholder.MODEL, model);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add(RequestHeader.GOOGLE_API_KEY, _configuration.ApiKey);

        var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);

        if (!httpResponse.IsSuccessStatusCode)
        {
            var envelope = await httpResponse.Content
                .ReadFromJsonAsync<GeminiErrorEnvelope>(cancellationToken: cancellationToken);
            throw new GeminiApiException(
                (int)httpResponse.StatusCode,
                envelope?.Error?.Message ?? $"Gemini API returned {(int)httpResponse.StatusCode}",
                envelope?.Error?.Status);
        }

        return (await httpResponse.Content.ReadFromJsonAsync<GenerateContentResponse>(cancellationToken: cancellationToken))!;
    }
}
