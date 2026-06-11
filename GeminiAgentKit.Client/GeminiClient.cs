using GeminiAgentKit.Client.Builders;
using GeminiAgentKit.Client.Factories;
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
    private List<FunctionDeclaration> _tools = new();

    public void AddTools(List<FunctionDeclaration> tools) => _tools = tools;

    public static FunctionDeclaration CreateFunction(string name, string description, Delegate handler)
        => FunctionFactory.Create(name, description, handler);

    public async Task<(GenerateContentRequest, GenerateContentResponse)> GenerateContentAsync(
        string instructions,
        GenerateContentRequest? request = null,
        string model = Model.Gemini35Flash)
    {
        request = RequestBuilder.Build(instructions, _tools, request);
        var response = await RunAgentLoopAsync(request, model);
        return (request, response);
    }

    public async Task<(GenerateContentRequest, GenerateContentResponse<T>)> GenerateContentAsync<T>(
        string instructions,
        GenerateContentRequest? request = null,
        string model = Model.Gemini35Flash)
    {
        request = RequestBuilder.Build<T>(instructions, _tools, request);
        var response = await RunAgentLoopAsync(request, model);
        return (request, response.ToTyped<T>());
    }

    private async Task<GenerateContentResponse> RunAgentLoopAsync(GenerateContentRequest request, string model)
    {
        var response = await GenerateResponseAsync(request, model);

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

            response = await GenerateResponseAsync(request, model);
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

    private async Task<GenerateContentResponse> GenerateResponseAsync(GenerateContentRequest request, string model)
    {
        var configurations = GeminiConfigurationBuilder.Build();
        var httpClient     = new HttpClient();

        configurations.BaseUrl = configurations.BaseUrl.Replace(Placeholder.MODEL, model);
        httpClient.DefaultRequestHeaders.Add(RequestHeader.GOOGLE_API_KEY, configurations.ApiKey);

        var response = await httpClient.PostAsJsonAsync(configurations.BaseUrl, request);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<GenerateContentResponse>())!;
    }
}
