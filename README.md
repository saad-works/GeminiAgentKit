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

The `{{model}}` placeholder is replaced at runtime with the model identifier you pass to `GenerateContentAsync()`.

---

## How to Use

### 1. Single-turn generation (no tools)

```csharp
var client = new GeminiClient();

var (_, response) = await client.GenerateContentAsync(
    "Summarize the history of the Roman Empire.",
    model: Model.Gemini25Flash
);

Console.WriteLine(response.Text);
```

### 2. Typed response

When you want the model to return structured JSON, use the generic overload. The response is deserialized to your type automatically.

```csharp
public record WeatherReport(string City, double TemperatureC, string Conditions);

var client = new GeminiClient();

var (_, response) = await client.GenerateContentAsync<WeatherReport>(
    "Return a JSON weather report for London right now.",
    model: Model.Gemini25Flash
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

// or scan an instance (for instance methods):
var tools = new MyTools();
client.AddTools(tools);

var (_, response) = await client.GenerateContentAsync(
    "What time is it, and what is 17 + 25?",
    model: Model.Gemini25Flash
);

Console.WriteLine(response.Text);
```

The client runs the full agent loop internally: if the model calls a function, the framework invokes it, feeds back the result, and calls the model again — repeating until the model returns a final text response.

### 4. Multi-turn conversations

Pass the `request` object back into subsequent calls to maintain conversation history:

```csharp
var client = new GeminiClient();
client.AddTools<FileSearcher>();
client.AddTools<FileReader>();

var (request, response) = await client.GenerateContentAsync(
    "List all .cs files in C:/MyProject",
    model: Model.Gemini25Flash
);
Console.WriteLine(response.Text);

// continue the same conversation
(request, response) = await client.GenerateContentAsync(
    "Now read the contents of Program.cs",
    request,
    model: Model.Gemini25Flash
);
Console.WriteLine(response.Text);
```

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

var (_, response) = await client.GenerateContentAsync(
    "Greet Alice.",
    model: Model.Gemini25Flash
);
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
| `Model.Gemini35Flash` | `gemini-1.5-flash` |
| `Model.Gemini31FlashLite` | `gemini-1.5-flash-lite` |
| `Model.Gemma431B` | `gemma-4-31b-it` |

Pass the constant (or any valid model string) as the `model` argument to `GenerateContentAsync`.

---

## Project Structure

```
GeminiAgentKit/
├── GeminiAgentKit.Types/          Core types
│   ├── Attributes/                [GeminiFunction] attribute
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
│   ├── Factories/                 FunctionFactory, ToolScanner
│   ├── Utilities/                 NameSanitizer
│   └── GeminiClient.cs            Main entry point
│
├── GeminiAgent.Tools/             Built-in tools
│   └── File/
│       ├── FileReader/            Read file content
│       └── FileSearcher/          Search files by pattern
│
└── GeminiAgentKit/                Demo console app
    ├── Program.cs
    ├── Utility/Loader.cs          Spinner UI
    └── appsettings.json           API key + base URL
```

---

## How It Works Internally

1. **Tool registration** — `ToolScanner` reflects on the target type, finds methods with `[GeminiFunction]`, and builds a `FunctionDeclaration` for each. The declaration holds the method name, description, a JSON Schema derived from the parameter types, and a delegate to invoke the method.

2. **Request building** — `RequestBuilder` assembles a `GenerateContentRequest` with the conversation history, system instruction, generation config, and the list of tool declarations.

3. **Agent loop** — `GeminiClient` POSTs the request to the Gemini API. If the response contains a `FunctionCall`, the matching declaration is looked up by name, its handler is invoked with the deserialized arguments, and the result is appended to the conversation as a `FunctionResponse`. This repeats until the model returns a text response.

4. **Schema generation** — `GeminiSchemaBuilder` walks C# parameter and property types recursively to produce a Gemini-compatible JSON Schema object, including `required` arrays and nested object definitions.

---

## Dependencies

| Package | Used for |
|---|---|
| `Microsoft.Extensions.Configuration` | Loading `appsettings.json` |
| `Microsoft.Extensions.Configuration.Json` | JSON config provider |
| `Microsoft.Extensions.Configuration.Binder` | Binding config to `GeminiConfiguration` |
| `NJsonSchema` | JSON Schema support |

All packages target .NET 10.

---

## License

MIT
