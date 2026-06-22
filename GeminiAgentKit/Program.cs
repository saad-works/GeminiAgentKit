using GeminiAgent.Tools.File.FileReader;
using GeminiAgent.Tools.File.FileSearcher;
using GeminiAgentKit.Client;
using GeminiAgentKit.Types.Chat;
using GeminiAgentKit.Types.System;
using GeminiAgentKit.Utility;

var geminiClient = new GeminiClient();

geminiClient.AddTools<FileSearcher>();
geminiClient.AddTools<FileReader>();

var chatHistory = new ChatHistory();

var response = await geminiClient.GenerateContentAsync(Model.Gemma431B, "How many planets are in the solar system?", chatHistory);

Console.WriteLine(response.Text);

while (true)
{
    Console.Write("\nYou: ");
    var query = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(query)) break;

    var task = geminiClient.GenerateContentAsync(Model.Gemini31FlashLite, query, chatHistory);
    var loader = Loader.Invoke(task);

    response = await task;

    await loader;

    Console.WriteLine($"\nModel: {response.Text}");
}
