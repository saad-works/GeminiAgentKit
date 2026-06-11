using GeminiAgentKit.Client.Factories;
using GeminiAgentKit.Types.Tools;

namespace GeminiAgent.Tools.File.FileReader;

public class FileReader : IBaseTool
{
    private const string _toolName = "Read a File";
    private const string _toolDescription = "Read the content of a file from a specified path";
    public static FunctionDeclaration GetTool()
    {
        return FunctionFactory.Create(_toolName, _toolDescription, ReadFileAsync);
    }

    public static async Task<FileReaderContext> ReadFileAsync(string fullPath)
    {
        var fileName = global::System.IO.Path.GetFileName(fullPath);

        if (!global::System.IO.File.Exists(fullPath))
        {
            throw new global::System.IO.FileNotFoundException(
                $"File '{fileName}' was not found.",
                fullPath);
        }

        var content = await global::System.IO.File.ReadAllTextAsync(fullPath);

        return new FileReaderContext
        {
            Name = fileName,
            FullPath = fullPath,
            FileContent = content
        };
    }
}
