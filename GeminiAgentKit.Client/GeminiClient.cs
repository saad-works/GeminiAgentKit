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
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new <see cref="GeminiClient"/> using the API key from the
    /// <c>GEMINI_API_KEY</c> (or <c>GOOGLE_API_KEY</c>) environment variable.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when neither environment variable is set.</exception>
    public GeminiClient() : this(ApiKeyFromEnvironment())
    {
    }

    /// <summary>
    /// Initializes a new <see cref="GeminiClient"/> with an API key.
    /// </summary>
    /// <param name="apiKey">Google AI Studio API key.</param>
    /// <param name="model">Default model used when a call does not specify one. Defaults to Gemini 3.5 Flash.</param>
    /// <param name="httpClient">Optional <see cref="HttpClient"/> (for proxies, retries or tests). A new one is created if omitted.</param>
    public GeminiClient(string apiKey, string? model = null, HttpClient? httpClient = null)
        : this(CreateConfiguration(apiKey, model), httpClient)
    {
    }

    /// <summary>
    /// Initializes a new <see cref="GeminiClient"/> with explicitly provided configuration.
    /// </summary>
    /// <param name="configuration">Gemini API configuration including the base URL and API key.</param>
    /// <param name="httpClient">Optional <see cref="HttpClient"/> (for proxies, retries or tests). A new one is created if omitted.</param>
    public GeminiClient(GeminiConfiguration configuration, HttpClient? httpClient = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (string.IsNullOrWhiteSpace(configuration.ApiKey))
            throw new ArgumentException("A Gemini API key is required.", nameof(configuration));

        _configuration = configuration;
        // Thinking models can take well over HttpClient's 100s default, so allow more when we own the client.
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
    }

    private static string ApiKeyFromEnvironment()
        => Environment.GetEnvironmentVariable("GEMINI_API_KEY")
           ?? Environment.GetEnvironmentVariable("GOOGLE_API_KEY")
           ?? throw new InvalidOperationException(
               "No Gemini API key found. Set the GEMINI_API_KEY environment variable, or pass the key to the GeminiClient constructor. " +
               "Get a key at https://aistudio.google.com/apikey");

    private static GeminiConfiguration CreateConfiguration(string apiKey, string? model)
    {
        var configuration = new GeminiConfiguration { ApiKey = apiKey };
        if (!string.IsNullOrWhiteSpace(model)) configuration.Model = model;
        return configuration;
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
    /// Sends <paramref name="instructions"/> using the client's default model and runs the agent loop
    /// until the model produces a final text response.
    /// </summary>
    /// <param name="instructions">The user message to send.</param>
    /// <param name="history">Conversation history to continue, or <c>null</c> to start a new conversation.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public Task<GenerateContentResponse> GenerateContentAsync(
        string instructions,
        ChatHistory? history = null,
        CancellationToken cancellationToken = default)
        => GenerateContentAsync(string.Empty, instructions, history, cancellationToken);

    /// <summary>
    /// Sends <paramref name="instructions"/> using the client's default model and deserializes the JSON reply to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the model's JSON response into.</typeparam>
    /// <param name="instructions">The user message to send.</param>
    /// <param name="history">Conversation history to continue, or <c>null</c> to start a new conversation.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public Task<GenerateContentResponse<T>> GenerateContentAsync<T>(
        string instructions,
        ChatHistory? history = null,
        CancellationToken cancellationToken = default)
        => GenerateContentAsync<T>(string.Empty, instructions, history, cancellationToken);

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

        for (var attempt = 0; ; attempt++)
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(request)
            };
            httpRequest.Headers.Add(RequestHeader.GOOGLE_API_KEY, _configuration.ApiKey);

            using var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (httpResponse.IsSuccessStatusCode)
                return (await httpResponse.Content.ReadFromJsonAsync<GenerateContentResponse>(cancellationToken: cancellationToken))!;

            var statusCode = (int)httpResponse.StatusCode;

            // Gemini returns 503 under load; that is worth retrying with backoff. 429 is not retried: it is usually a quota limit that a retry cannot fix and only burns more quota.
            if (IsTransient(statusCode) && attempt < _configuration.MaxRetries)
            {
                await Task.Delay(_configuration.RetryBaseDelay * Math.Pow(2, attempt), cancellationToken);
                continue;
            }

            var envelope = await httpResponse.Content
                .ReadFromJsonAsync<GeminiErrorEnvelope>(cancellationToken: cancellationToken);
            throw new GeminiApiException(
                statusCode,
                envelope?.Error?.Message ?? $"Gemini API returned {statusCode}",
                envelope?.Error?.Status);
        }
    }

    private static bool IsTransient(int statusCode) => statusCode is 500 or 502 or 503 or 504;
}
