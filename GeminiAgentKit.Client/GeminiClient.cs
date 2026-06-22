using GeminiAgentKit.Client.Builders;
using GeminiAgentKit.Client.Exceptions;
using GeminiAgentKit.Client.Factories;
using GeminiAgentKit.Types.Chat;
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

/// <summary>
/// Client for building conversational agents backed by the Google Gemini API.
/// Handles tool registration, schema generation, and the function-calling loop automatically.
/// </summary>
public class GeminiClient
{
    private readonly List<FunctionDeclaration> _tools = [];
    private readonly GeminiConfiguration _configuration;
    private readonly HttpClient _httpClient = new();
    /// <summary>
    /// Initializes a new <see cref="GeminiClient"/> loading configuration from <c>appsettings.json</c>.
    /// </summary>
    public GeminiClient() : this(GeminiConfigurationBuilder.Build())
    {
    }

    /// <summary>
    /// Initializes a new <see cref="GeminiClient"/> with explicitly provided configuration.
    /// </summary>
    /// <param name="configuration">Gemini API configuration including the base URL and API key.</param>
    public GeminiClient(GeminiConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>Registers a list of pre-built tool declarations.</summary>
    public void AddTools(List<FunctionDeclaration> tools) => _tools.AddRange(tools);

    /// <summary>
    /// Scans <typeparamref name="T"/> for methods decorated with <c>[GeminiFunction]</c> and registers them as tools.
    /// </summary>
    public void AddTools<T>() where T : new() => _tools.AddRange(ToolScanner.Scan<T>());

    /// <summary>
    /// Scans <paramref name="instance"/> for methods decorated with <c>[GeminiFunction]</c> and registers them as tools.
    /// </summary>
    public void AddTools(object instance) => _tools.AddRange(ToolScanner.Scan(instance));

    /// <summary>Creates a single tool declaration from a delegate without requiring a dedicated class.</summary>
    /// <param name="name">Tool name sent to the model.</param>
    /// <param name="description">Plain-English description of what the tool does.</param>
    /// <param name="handler">Delegate that implements the tool.</param>
    public static FunctionDeclaration CreateFunction(string name, string description, Delegate handler)
        => FunctionFactory.Create(name, description, handler);

    /// <summary>
    /// Sends <paramref name="instructions"/> and runs the agent loop until the model produces a final text response.
    /// Any tool calls the model makes are handled transparently.
    /// </summary>
    /// <param name="model">The Gemini model to use (e.g. "gemini-2.0-flash"). If <c>null</c> or whitespace, the default model from configuration is used.</param>
    /// <param name="instructions">The user message to send.</param>
    /// <param name="history">Conversation history to continue, or <c>null</c> to start a new conversation.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The model's final text response.</returns>
    /// <exception cref="GeminiApiException">Thrown when the Gemini API returns a non-success status code.</exception>
    public async Task<GenerateContentResponse> GenerateContentAsync(
        string model,
        string instructions,
        ChatHistory? history = null,
        CancellationToken cancellationToken = default)
    {
        var request = RequestBuilder.Build(instructions, _tools, history);
        var response = await RunAgentLoopAsync(model, request, cancellationToken);

        history?.AddUserMessage(instructions);
        history?.AddModelMessage(response.Text ?? string.Empty);

        return response;
    }

    /// <summary>
    /// Sends <paramref name="instructions"/> and runs the agent loop, deserializing the final JSON response to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the model's JSON response into.</typeparam>
    /// <param name="model">The Gemini model to use (e.g. "gemini-2.0-flash"). If <c>null</c> or whitespace, the default model from configuration is used.</param>
    /// <param name="instructions">The user message to send.</param>
    /// <param name="history">Conversation history to continue, or <c>null</c> to start a new conversation.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The typed response.</returns>
    /// <exception cref="GeminiApiException">Thrown when the Gemini API returns a non-success status code.</exception>
    public async Task<GenerateContentResponse<T>> GenerateContentAsync<T>(
        string model,
        string instructions,
        ChatHistory? history = null,
        CancellationToken cancellationToken = default)
    {
        var request = RequestBuilder.Build<T>(instructions, _tools, history);
        var response = await RunAgentLoopAsync(model, request, cancellationToken);

        history?.AddUserMessage(instructions);
        history?.AddModelMessage(response.Text ?? string.Empty);

        return response.ToTyped<T>();
    }

    private async Task<GenerateContentResponse> RunAgentLoopAsync(
        string model,
        GenerateContentRequest request,
        CancellationToken cancellationToken)
    {
        var response = await GenerateResponseAsync(model, request, cancellationToken);

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

            response = await GenerateResponseAsync(model, request, cancellationToken);
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
        string model,
        GenerateContentRequest request,
        CancellationToken cancellationToken)
    {
        model = string.IsNullOrWhiteSpace(model) ? _configuration.Model : model;

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
