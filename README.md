# GeminiAgentKit

[![CI](https://github.com/saad-works/GeminiAgentKit/actions/workflows/ci.yml/badge.svg)](https://github.com/saad-works/GeminiAgentKit/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/GeminiAgentKit.Client?logo=nuget)](https://www.nuget.org/packages/GeminiAgentKit.Client)
[![License: MIT](https://img.shields.io/badge/license-MIT-green.svg)](./LICENSE.txt)
[![.NET](https://img.shields.io/badge/.NET-8%20%7C%209%20%7C%2010-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)

**Gemini function calling in C# without the plumbing.** Write a normal method, add an attribute, and the agent loop (schema generation, argument parsing, tool dispatch, feeding results back to the model) is handled for you.

```csharp
using GeminiAgentKit.Client;
using GeminiAgentKit.Types.Attributes;

var client = new GeminiClient();          // reads GEMINI_API_KEY
client.AddTools<MyTools>();

var response = await client.GenerateContentAsync("What time is it, and what is 17 + 25?");
Console.WriteLine(response.Text);

public class MyTools
{
    [GeminiFunction("Get the current UTC time as an ISO 8601 string")]
    public static string GetCurrentTime() => DateTime.UtcNow.ToString("O");

    [GeminiFunction("Add two numbers")]
    public static double Add(double a, double b) => a + b;
}
```

That is the whole program. The model decides to call `GetCurrentTime` and `Add`, the kit runs them, returns the results to the model, and you get the final answer.

**See it do real work:** [`samples/CodeReviewAgent`](./samples/CodeReviewAgent) is an AI code reviewer for your git diff, about 120 lines built on this kit.

![CodeReviewAgent finding a SQL injection, a resource leak, an off-by-one and a divide-by-zero in a git diff](https://raw.githubusercontent.com/saad-works/GeminiAgentKit/master/media/review-demo.gif)

## Why GeminiAgentKit

- **Tiny surface area.** One client class, one attribute. No DI container, no config file required.
- **Zero setup.** Set `GEMINI_API_KEY`, or pass the key to the constructor.
- **Automatic JSON Schema.** Parameter types (primitives, enums, collections, nullables, nested objects) are turned into Gemini-compatible schemas by reflection.
- **Typed responses.** Ask for structured JSON and get a C# object back.
- **Multi-turn chat** with a caller-owned `ChatHistory`.
- **Async tools** (`Task<T>`) work out of the box.
- **Multi-targets .NET 8, 9 and 10.** No preview dependencies.

## Install

```bash
dotnet add package GeminiAgentKit.Client
dotnet add package GeminiAgentKit.Tools   # optional: built-in file tools
```

Get an API key from [Google AI Studio](https://aistudio.google.com/apikey), then:

```bash
# macOS / Linux
export GEMINI_API_KEY="your-key"

# Windows (PowerShell)
$env:GEMINI_API_KEY = "your-key"
```

Or pass it explicitly (and optionally pick a default model):

```csharp
var client = new GeminiClient("your-key", model: Model.Gemini38Flash);
```

`GOOGLE_API_KEY` is also accepted as the environment variable name. The default model is `gemini-3.5-flash`. Google retires older models for new API users regularly (2.5 Flash and Pro already are), so pass a model explicitly if you need a specific one.

---

## How to Use

### 1. Single-turn generation

```csharp
var client = new GeminiClient();

var response = await client.GenerateContentAsync("Summarize the history of the Roman Empire.");
Console.WriteLine(response.Text);
```

Pass a model per call when you need one:

```csharp
var response = await client.GenerateContentAsync(Model.Gemini25Pro, "Explain monads simply.");
```

### 2. Typed response

When you want structured JSON, use the generic overload. The response is deserialized to your type automatically.

```csharp
public record WeatherReport(string City, double TemperatureC, string Conditions);

var response = await client.GenerateContentAsync<WeatherReport>(
    "Return a JSON weather report for London right now.");

WeatherReport report = response.Result;
Console.WriteLine($"{report.City}: {report.TemperatureC}°C, {report.Conditions}");
```

### 3. Tools with `[GeminiFunction]`

Decorate any public method (static or instance) with `[GeminiFunction]`. The signature is reflected into a JSON schema automatically.

```csharp
public class MyTools
{
    [GeminiFunction("Get the current UTC time as an ISO 8601 string")]
    public static string GetCurrentTime() => DateTime.UtcNow.ToString("O");

    [GeminiFunction("Add two numbers and return the result")]
    public static double Add(double a, double b) => a + b;
}
```

Register them before calling `GenerateContentAsync`:

```csharp
client.AddTools<MyTools>();           // scan a type (needs a parameterless constructor)
client.AddTools(new MyTools());       // or scan an instance (for constructor-injected dependencies)
```

The client runs the full agent loop internally: if the model calls a function, the kit invokes it, feeds back the result, and calls the model again until it returns a final text response.

### 4. Multi-turn conversations

Create a `ChatHistory` and pass it to each call. The client reads prior messages from it and appends the new user message and model reply after each turn.

```csharp
client.AddTools<FileSearcher>();
client.AddTools<FileReader>();

var history = new ChatHistory();

var response = await client.GenerateContentAsync("List all .cs files in C:/MyProject", history);
Console.WriteLine(response.Text);

// Same conversation: history carries the context
response = await client.GenerateContentAsync("Now read the contents of Program.cs", history);
Console.WriteLine(response.Text);
```

Omit `history` for stateless single-turn calls.

### 5. Dynamic tools with `CreateFunction`

For one-off tools where you don't want a dedicated class:

```csharp
var greet = GeminiClient.CreateFunction(
    name:        "greet_user",
    description: "Greet the user by name",
    handler:     (string name) => $"Hello, {name}!");

client.AddTools([greet]);

var response = await client.GenerateContentAsync("Greet Alice.");
```

### 6. Error handling

API failures throw `GeminiApiException` with the HTTP status code and Gemini's error status:

```csharp
try
{
    await client.GenerateContentAsync("Hi");
}
catch (GeminiApiException ex)
{
    Console.WriteLine($"{ex.StatusCode} {ex.Status}: {ex.Message}");
}
```

### 7. Retries

Transient server errors (HTTP 500, 502, 503, 504) are retried automatically with exponential backoff, 3 times by default. Tune this with `GeminiConfiguration.MaxRetries` and `RetryBaseDelay` (set `MaxRetries = 0` to disable). Rate-limit and quota errors (429) are not retried.

### 8. Custom `HttpClient`

Pass your own `HttpClient` for proxies, logging or tests:

```csharp
var client = new GeminiClient("your-key", httpClient: myHttpClient);
```

---

## Chat Management

`ChatHistory` is the mechanism for maintaining context across turns. It is a simple, caller-owned object that stores an ordered list of `Message` entries.

**How it works:**

1. Before each API call, `RequestBuilder` replays all messages in `history.Messages` into the request, restoring the conversation context.
2. After the agent loop returns a final text reply, `GeminiClient` appends the user's message and the model's reply to `history`.

**Key properties:**

- **Caller-owned.** You hold the `ChatHistory` reference. The client reads from and writes to it, but never stores it.
- **Stateless client.** One `GeminiClient` can serve many independent conversations with separate `ChatHistory` objects.
- **Transparent to tool calls.** Intermediate function calls made during the agent loop are not written to `ChatHistory`. Only the final user message and model text reply are recorded.

**Branching conversations:** because `ChatHistory` is a plain object, you can fork a conversation by cloning the message list:

```csharp
var fork = new ChatHistory { Messages = new List<Message>(history.Messages) };

var branchA = await client.GenerateContentAsync("Take this in direction A", fork);
```

---

## Built-in Tools

The `GeminiAgentKit.Tools` package ships two ready-to-use file tools.

| Tool | Registration | What it does |
|---|---|---|
| `FileSearcher` | `client.AddTools<FileSearcher>()` | Recursively lists files under `rootDirectory`, filtered by `searchPattern` (e.g. `*.cs`). Returns names, paths and last-modified timestamps. |
| `FileReader` | `client.AddTools<FileReader>()` | Reads the full text of a file by path. Returns the file name, full path and content. |

---

## Writing Your Own Tools

Any class can expose tools. The only requirement is a `[GeminiFunction]` attribute on each method you want to expose.

### Supported parameter types

- Primitives: `string`, `bool`, `int`, `long`, `float`, `double`
- Enums (serialized as strings)
- Collections: `IEnumerable<T>`, `List<T>`, arrays
- Nullable types: `T?`
- Complex objects: public properties are reflected recursively

### Example: web tool

```csharp
public class WebTools
{
    private readonly HttpClient _http = new();

    [GeminiFunction("Fetch the HTML content of a URL")]
    public async Task<string> FetchPageAsync(string url) => await _http.GetStringAsync(url);
}

client.AddTools(new WebTools());
```

### Custom tool name

By default the tool name is derived from the method name. Override it with the `Name` property:

```csharp
[GeminiFunction("Search the product catalog", Name = "search_products")]
public List<Product> Search(string query) { ... }
```

### Return types

Tools can return any serializable type. The return value is serialized to JSON and sent back to the model as a function response. `Task<T>` is awaited automatically.

> **Safety note:** tools run with your process's permissions. Don't expose tools (file access, shell, SQL) to models you don't trust with those capabilities, and validate arguments inside your tools.

---

## Available Models

Constants are in `GeminiAgentKit.Types.System.Model`. You can also pass any model ID string.

| Constant | Model ID |
|---|---|
| `Model.Gemini25Pro` | `gemini-2.5-pro` (deprecated for new users) |
| `Model.Gemini38Flash` | `gemini-3.8-flash` |
| `Model.Gemini25Flash` | `gemini-2.5-flash` (deprecated for new users) |
| `Model.Gemini35Flash` | `gemini-3.5-flash` (default) |
| `Model.Gemini31FlashLite` | `gemini-3.1-flash-lite` |
| `Model.Gemma431B` | `gemma-4-31b-it` |

---

## Try the demo

A small console chat agent with file search and read tools lives in `GeminiAgentKit/`:

```bash
git clone https://github.com/saad-works/GeminiAgentKit.git
cd GeminiAgentKit
export GEMINI_API_KEY="your-key"      # or $env:GEMINI_API_KEY in PowerShell
dotnet run --project GeminiAgentKit
```

## Project Structure

```
GeminiAgentKit/
├── GeminiAgentKit.Types/     Core types: requests, responses, ChatHistory, schema builder, [GeminiFunction]
├── GeminiAgentKit.Client/    GeminiClient, tool scanning, request building, exceptions
├── GeminiAgent.Tools/        Built-in tools (published as GeminiAgentKit.Tools)
├── GeminiAgentKit.Tests/     Unit tests (no network; uses a fake HTTP handler)
├── samples/CodeReviewAgent/  AI code reviewer for git diffs
└── GeminiAgentKit/           Demo console app
```

## How It Works Internally

1. **Tool registration.** `ToolScanner` reflects on the target type, finds methods with `[GeminiFunction]`, and builds a `FunctionDeclaration` for each: name, description, a JSON Schema from the parameter types, and an invoker delegate.
2. **Request building.** `RequestBuilder` assembles a `GenerateContentRequest` with the system instruction and tool declarations. If a `ChatHistory` is provided, its messages are replayed first.
3. **Agent loop.** `GeminiClient` POSTs the request to the Gemini API. If the response contains a `FunctionCall`, the matching declaration is invoked with the deserialized arguments, and the result is appended to the request as a `FunctionResponse`. This repeats until the model returns text.
4. **History update.** Once a final reply arrives, the user's instruction and the model's response are appended to `ChatHistory`.
5. **Schema generation.** `GeminiSchemaBuilder` walks C# parameter and property types recursively to produce a Gemini-compatible JSON Schema, including `required` arrays and nested objects.

## Contributing

```bash
dotnet build
dotnet test
```

Issues and pull requests are welcome. Please include a test with behaviour changes.

## License

[MIT](./LICENSE.txt)
