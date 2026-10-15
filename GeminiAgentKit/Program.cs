using GeminiAgent.Tools.File.FileReader;
using GeminiAgent.Tools.File.FileSearcher;
using GeminiAgentKit.Client;
using GeminiAgentKit.Types.Chat;
using GeminiAgentKit.Utility;

// Demo: a console chat agent that can search and read files on disk.
// Set GEMINI_API_KEY first (https://aistudio.google.com/apikey), then `dotnet run --project GeminiAgentKit`.
var geminiClient = new GeminiClient();

geminiClient.AddTools<FileSearcher>();
geminiClient.AddTools<FileReader>();

var chatHistory = new ChatHistory();

Console.WriteLine("Ask me anything, or about files on this machine. Press Enter on an empty line to quit.");

while (true)
{
    Console.Write("\nYou: ");
    var query = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(query)) break;

    var task = geminiClient.GenerateContentAsync(query, chatHistory);
    var loader = Loader.Invoke(task);

    var response = await task;

    await loader;

    Console.WriteLine($"\nModel: {response.Text}");
}
