using System.Net;
using GeminiAgentKit.Client;
using GeminiAgentKit.Client.Exceptions;
using GeminiAgentKit.Types.Attributes;
using GeminiAgentKit.Types.Chat;
using GeminiAgentKit.Types.System;

namespace GeminiAgentKit.Tests;

public class GeminiClientTests
{
    private static (GeminiClient Client, FakeGeminiHandler Handler) Create(string? model = null)
    {
        var handler = new FakeGeminiHandler();
        return (new GeminiClient("test-key", model, new HttpClient(handler)), handler);
    }

    public class MathTools
    {
        [GeminiFunction("Add two numbers and return the result")]
        public static double Add(double a, double b) => a + b;
    }

    [Fact]
    public async Task PlainPrompt_ReturnsModelText_AndSendsApiKeyHeader()
    {
        var (client, handler) = Create();
        handler.EnqueueText("Hello!");

        var response = await client.GenerateContentAsync("Hi");

        Assert.Equal("Hello!", response.Text);
        Assert.Single(handler.Requests);
        Assert.Equal("test-key", handler.Requests[0].Headers.GetValues("x-goog-api-key").Single());
    }

    [Fact]
    public async Task DefaultModel_IsUsedInUrl_WhenNoneSpecified()
    {
        var (client, handler) = Create();
        handler.EnqueueText("ok");

        await client.GenerateContentAsync("Hi");

        Assert.Contains($"models/{Model.Gemini25Flash}:generateContent", handler.Requests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task ExplicitModel_OverridesDefault()
    {
        var (client, handler) = Create(model: "gemini-custom");
        handler.EnqueueText("ok").EnqueueText("ok");

        await client.GenerateContentAsync("Hi");
        await client.GenerateContentAsync(Model.Gemini25Pro, "Hi");

        Assert.Contains("models/gemini-custom:", handler.Requests[0].RequestUri!.ToString());
        Assert.Contains($"models/{Model.Gemini25Pro}:", handler.Requests[1].RequestUri!.ToString());
    }

    [Fact]
    public async Task ToolCall_IsExecuted_AndResultFedBackToModel()
    {
        var (client, handler) = Create();
        client.AddTools<MathTools>();
        handler.EnqueueFunctionCall("Add", """{"a":17,"b":25}""").EnqueueText("The answer is 42.");

        var response = await client.GenerateContentAsync("What is 17 + 25?");

        Assert.Equal("The answer is 42.", response.Text);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("functionResponse", handler.RequestBodies[1]);
        Assert.Contains("42", handler.RequestBodies[1]);
    }

    [Fact]
    public async Task ChatHistory_RecordsUserAndModelMessages()
    {
        var (client, handler) = Create();
        handler.EnqueueText("Hi there");
        var history = new ChatHistory();

        await client.GenerateContentAsync("Hello", history);

        Assert.Equal(2, history.Messages.Count);
    }

    [Fact]
    public async Task TypedResponse_DeserializesJson_AndStripsCodeFences()
    {
        var (client, handler) = Create();
        handler.Enqueue("""{"candidates":[{"content":{"role":"model","parts":[{"text":"```json\n{\"city\":\"London\",\"temperatureC\":12.5}\n```"}]}}]}""");

        var response = await client.GenerateContentAsync<Weather>("Weather in London?");

        Assert.Equal("London", response.Result!.City);
        Assert.Equal(12.5, response.Result.TemperatureC);
    }

    [Fact]
    public async Task ApiError_ThrowsGeminiApiException_WithStatusAndMessage()
    {
        var (client, handler) = Create();
        handler.Enqueue("""{"error":{"code":400,"message":"API key not valid.","status":"INVALID_ARGUMENT"}}""", HttpStatusCode.BadRequest);

        var ex = await Assert.ThrowsAsync<GeminiApiException>(() => client.GenerateContentAsync("Hi"));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal("INVALID_ARGUMENT", ex.Status);
        Assert.Contains("API key not valid", ex.Message);
    }

    [Fact]
    public void EmptyApiKey_Throws()
        => Assert.Throws<ArgumentException>(() => new GeminiClient(""));

    [Fact]
    public void ParameterlessConstructor_ReadsEnvironmentVariable_OrThrowsHelpfulError()
    {
        var gemini = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        var google = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
        try
        {
            Environment.SetEnvironmentVariable("GEMINI_API_KEY", null);
            Environment.SetEnvironmentVariable("GOOGLE_API_KEY", null);
            var ex = Assert.Throws<InvalidOperationException>(() => new GeminiClient());
            Assert.Contains("GEMINI_API_KEY", ex.Message);

            Environment.SetEnvironmentVariable("GEMINI_API_KEY", "from-env");
            _ = new GeminiClient(); // must not throw
        }
        finally
        {
            Environment.SetEnvironmentVariable("GEMINI_API_KEY", gemini);
            Environment.SetEnvironmentVariable("GOOGLE_API_KEY", google);
        }
    }

    private record Weather(string City, double TemperatureC);
}
