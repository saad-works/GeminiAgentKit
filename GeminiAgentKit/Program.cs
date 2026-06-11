using GeminiAgent.Tools.File.FileReader;
using GeminiAgent.Tools.File.FileSearcher;
using GeminiAgentKit.Client;
using GeminiAgentKit.Types.System;
using GeminiAgentKit.Utility;

var geminiClient = new GeminiClient();

var searchTool = FileSearcher.GetTool();
var readTool = FileReader.GetTool();

geminiClient.AddTools([readTool, searchTool]);

var (request, response) = await geminiClient.GenerateContentAsync("read all the files in the this directory", model: Model.Gemma431B);

Console.WriteLine(response.Text);

while (true)
{
    Console.Write("\nYou: ");
    var query = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(query)) break;

    var task = geminiClient.GenerateContentAsync(query, request, model: Model.Gemma431B);

    var loader = Loader.Invoke(task);

    (request, response) = await task;
    
    await loader;
    
    Console.WriteLine($"\nModel: {response.Text}");
}
