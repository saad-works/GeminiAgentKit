# GeminiAgentKit

A C# framework for building agentic applications on top of Google's Gemini API. Handles tool registration, automatic schema generation, multi-turn conversation state, and the function-calling loop — so you can focus on writing tools, not wiring the API.

---

## Objective

Gemini's function-calling API is powerful but verbose to use directly: you have to define JSON schemas for every tool, serialize arguments, dispatch calls, serialize responses, and loop until the model stops calling functions. GeminiAgentKit automates all of that.

The goal is a minimal surface area where you:

1. Write plain C# methods decorated with `[GeminiFunction]`
2. Register them with a `GeminiClient`
3. Call `GenerateContentAsync()` and get back a response — with any tool calls already handled transparently

The framework targets **.NET 10** and is organized as four focused projects:

| Project | Role |
|---|---|
| `GeminiAgentKit.Types` | Core types: requests, responses, content, tool definitions |
| `GeminiAgentKit.Client` | `GeminiClient`, tool scanning, schema generation, request building |
| `GeminiAgent.Tools` | Built-in tools (file search, file read) |
| `GeminiAgentKit` | Console demo app |

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A [Google AI Studio](https://aistudio.google.com/) API key

---

## Configuration

Add your API key to `GeminiAgentKit/appsettings.json`:

```json
{
  "GeminiConfiguration": {
    "BaseUrl": "https://generativelanguage.googleapis.com/v1beta/models/{{model}}:generateContent",
    "ApiKey": "YOUR_API_KEY_HERE"
  }
}
```

The `{{model}}` placeholder is replaced at runtime with the configured model identifier.

---

## How to Use

### 1. Single-turn generation (no tools)

```csharp
var client = new GeminiClient();

var response = await client.GenerateContentAsync(Model.Gemini25Flash, "Summarize the history of the Roman Empire.");

Console.WriteLine(response.Text);
```

### 2. Typed response

When you want the model to return structured JSON, use the generic overload. The response is deserialized to your type automatically.

```csharp
public record WeatherReport(string City, double TemperatureC, string Conditions);

var client = new GeminiClient();

var response = await client.GenerateContentAsync<WeatherReport>(
    Model.Gemini25Flash,
    "Return a JSON weather report for London right now."
);

WeatherReport report = response.Result;
Console.WriteLine($"{report.City}: {report.TemperatureC}°C, {report.Conditions}");
```

### 3. Registering tools with `[GeminiFunction]`

Decorate any public method — static or instance — with `[GeminiFunction]`. The framework reflects on the signature to build the JSON schema automatically.

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
var client = new GeminiClient();
client.AddTools<MyTools>();                 // scan a type (static methods)

// or scan an instance (for instance methods or constructor-injected dependencies):
client.AddTools(new MyTools());

var response = await client.GenerateContentAsync(Model.Gemini25Flash, "What time is it, and what is 17 + 25?");

Console.WriteLine(response.Text);
```

The client runs the full agent loop internally: if the model calls a function, the framework invokes it, feeds back the result, and calls the model again — repeating until the model returns a final text response.

### 4. Multi-turn conversations

Create a `ChatHistory` and pass it to each call. The client reads prior messages from it to provide context, and appends the new user message and model reply after each turn.

```csharp
var client = new GeminiClient();
client.AddTools<FileSearcher>();
client.AddTools<FileReader>();

var history = new ChatHistory();

var response = await client.GenerateContentAsync(
    Model.Gemini25Flash,
    "List all .cs files in C:/MyProject",
    history
);
Console.WriteLine(response.Text);

// Continue the same conversation — history carries the context automatically
response = await client.GenerateContentAsync(
    Model.Gemini25Flash,
    "Now read the contents of Program.cs",
    history
);
Console.WriteLine(response.Text);
```

Omit `history` (or pass `null`) for stateless single-turn calls.

### 5. Dynamic tools with `FunctionFactory`

For one-off tools where you don't want a dedicated class:

```csharp
var greet = GeminiClient.CreateFunction(
    name:        "greet_user",
    description: "Greet the user by name",
    handler:     (string name) => $"Hello, {name}!"
);

var client = new GeminiClient();
client.AddTools([greet]);

var response = await client.GenerateContentAsync(Model.Gemini25Flash, "Greet Alice.");
```

---

## Chat Management

`ChatHistory` is the primary mechanism for maintaining context across multiple turns. It is a simple, caller-owned object that stores an ordered list of `Message` entries.

```csharp
public class ChatHistory
{
    public List<Message> Messages { get; set; }

    public void AddUserMessage(string content);
    public void AddModelMessage(string content);
}
```

**How it works:**

1. Before each API call, `RequestBuilder` replays all messages in `history.Messages` into the request's `Contents` list, restoring the full conversation context.
2. After the agent loop completes and the model returns a final text reply, `GeminiClient` appends the user's message and the model's reply to `history` automatically.

**Key properties:**

- **Caller-owned** — you hold the `ChatHistory` reference. The client reads from and writes to it, but never stores it internally.
- **Stateless client** — a single `GeminiClient` instance can serve multiple independent conversations by using separate `ChatHistory` objects.
- **Transparent to tool calls** — intermediate function calls made during the agent loop are not written to `ChatHistory`. Only the final user message and model text reply are recorded.

**Branching conversations:**

Because `ChatHistory` is a plain object, you can fork conversations by cloning the message list:

```csharp
var fork = new ChatHistory
{
    Messages = new List<Message>(history.Messages)
};

// Continue independently from the same point
var branchA = await client.GenerateContentAsync("Take this in direction A", fork);
```

**Inspecting history:**

```csharp
foreach (var message in history.Messages)
    Console.WriteLine($"{message.Role}: {message.Text}");
```

---

## Built-in Tools

The `GeminiAgent.Tools` project ships two ready-to-use file tools.

### FileSearcher

```csharp
client.AddTools<FileSearcher>();
```

Recursively lists files under a directory. The model can call it with:
- `rootDirectory` — path to search
- `searchPattern` — glob filter, e.g. `*.cs` (defaults to `*.*`)

Returns file names, paths, and last-modified timestamps.

### FileReader

```csharp
client.AddTools<FileReader>();
```

Reads the full text content of a file by path.

Returns the file name, full path, and content string.

---

## Extensibility: Writing Your Own Tools

Any class can expose tools. The only requirement is a `[GeminiFunction]` attribute on each method you want to expose.

### Supported parameter types

The schema builder handles:

- Primitives: `string`, `bool`, `int`, `long`, `float`, `double`
- Enums (serialized as strings)
- Collections: `IEnumerable<T>`, `List<T>`, arrays
- Nullable types: `T?`
- Complex objects: public properties are reflected recursively

### Example: web and database tools

```csharp
public class WebTools
{
    private readonly HttpClient _http = new();

    [GeminiFunction("Fetch the HTML content of a URL")]
    public async Task<string> FetchPageAsync(string url)
    {
        return await _http.GetStringAsync(url);
    }
}

public class DatabaseTools
{
    [GeminiFunction("Run a read-only SQL query and return rows as JSON")]
    public async Task<string> QueryAsync(string sql)
    {
        // execute query, serialize rows
        return JsonSerializer.Serialize(results);
    }
}

var client = new GeminiClient();
client.AddTools(new WebTools());
client.AddTools(new DatabaseTools());
```

### Custom tool name

By default the tool name is derived from the method name. Override it with the `Name` property:

```csharp
[GeminiFunction("Search the product catalog", Name = "search_products")]
public List<Product> Search(string query) { ... }
```

### Return types

Tools can return any serializable type. The framework serializes the return value to JSON and sends it back to the model as a function response. `Task<T>` and `Task` (void async) are both handled.

---

## Available Models

Constants are in `GeminiAgentKit.Types.System.Model`:

| Constant | Model ID |
|---|---|
| `Model.Gemini25Pro` | `gemini-2.5-pro` |
| `Model.Gemini25Flash` | `gemini-2.5-flash` |
| `Model.Gemini35Flash` | `gemini-3.5-flash` |
| `Model.Gemini31FlashLite` | `gemini-3.1-flash-lite` |
| `Model.Gemma431B` | `gemma-4-31b-it` (default) |

---

## Project Structure

```
GeminiAgentKit/
├── GeminiAgentKit.Types/          Core types
│   ├── Attributes/                [GeminiFunction] attribute
│   ├── Chat/                      ChatHistory, Message
│   ├── Configurations/            GeminiConfiguration, GenerationConfig
│   ├── Contents/                  Content, Part
│   ├── Requests/                  GenerateContentRequest
│   ├── Responses/                 GenerateContentResponse, Candidate, UsageMetadata
│   ├── Schema/                    GeminiSchemaBuilder (C# → JSON Schema)
│   ├── System/                    Model, Role constants
│   └── Tools/                     FunctionDeclaration, FunctionCall, FunctionResponse, Tool
│
├── GeminiAgentKit.Client/         Client and tool infrastructure
│   ├── Builders/                  GeminiConfigurationBuilder, RequestBuilder
│   ├── Exceptions/                GeminiApiException
│   ├── Factories/                 FunctionFactory, ToolScanner
│   ├── Utilities/                 NameSanitizer
│   └── GeminiClient.cs            Main entry point
│
├── GeminiAgent.Tools/             Built-in tools
│   └── File/
│       ├── FileReader/            Read file content
│       └── FileSearcher/          Search files by pattern
│
└── GeminiAgentKit/                Console app
    ├── Program.cs
    ├── Utility/Loader.cs          Spinner UI
    └── appsettings.json           API key + base URL
```

---

## How It Works Internally

1. **Tool registration** — `ToolScanner` reflects on the target type, finds methods with `[GeminiFunction]`, and builds a `FunctionDeclaration` for each. The declaration holds the method name, description, a JSON Schema derived from the parameter types, and an invoker delegate.

2. **Request building** — `RequestBuilder` assembles a `GenerateContentRequest` with the system instruction and tool declarations. If a `ChatHistory` is provided, its messages are replayed into `Contents` before the new user message is appended.

3. **Agent loop** — `GeminiClient` POSTs the request to the Gemini API. If the response contains a `FunctionCall`, the matching declaration is looked up by name, its handler is invoked with the deserialized arguments, and the result is appended to the request as a `FunctionResponse`. This repeats until the model returns a text response.

4. **History update** — once the agent loop produces a final text reply, `GeminiClient` appends the user's instruction and the model's response to `ChatHistory`, keeping it in sync for the next turn.

5. **Schema generation** — `GeminiSchemaBuilder` walks C# parameter and property types recursively to produce a Gemini-compatible JSON Schema object, including `required` arrays and nested object definitions.

---

## Dependencies

| Package | Used for |
|---|---|
| `Microsoft.Extensions.Configuration` | Loading `appsettings.json` |
| `Microsoft.Extensions.Configuration.Json` | JSON config provider |
| `Microsoft.Extensions.Configuration.Binder` | Binding config to `GeminiConfiguration` |

All packages target .NET 10.

---

## License

MIT
