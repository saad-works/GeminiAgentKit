using System.Net;
using System.Text;

namespace GeminiAgentKit.Tests;

/// <summary>Returns queued canned responses and records every request, so tests never hit the network.</summary>
internal sealed class FakeGeminiHandler : HttpMessageHandler
{
    private readonly Queue<(HttpStatusCode Status, string Body)> _responses = new();

    public List<HttpRequestMessage> Requests { get; } = [];
    public List<string> RequestBodies { get; } = [];

    public FakeGeminiHandler Enqueue(string body, HttpStatusCode status = HttpStatusCode.OK)
    {
        _responses.Enqueue((status, body));
        return this;
    }

    public FakeGeminiHandler EnqueueText(string text)
        => Enqueue($$"""{"candidates":[{"content":{"role":"model","parts":[{"text":"{{text}}"}]},"finishReason":"STOP"}]}""");

    public FakeGeminiHandler EnqueueFunctionCall(string name, string argsJson)
        => Enqueue($$$"""{"candidates":[{"content":{"role":"model","parts":[{"functionCall":{"name":"{{{name}}}","args":{{{argsJson}}}}}]}}]}""");

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        RequestBodies.Add(request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken));

        var (status, body) = _responses.Dequeue();
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }
}
