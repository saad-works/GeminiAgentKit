using GeminiAgentKit.Types.Attributes;

namespace GeminiAgent.Tools.File.FileReader;

public class FileReader
{
    [GeminiFunction("Read the content of a file from a specified path")]
    public static async Task<FileReaderContext> ReadFileAsync(string fullPath)
    {
        var fileName = Path.GetFileName(fullPath);

        if (!global::System.IO.File.Exists(fullPath))
            throw new FileNotFoundException($"File '{fileName}' was not found.", fullPath);

        return new FileReaderContext
        {
            Name = fileName,
            FullPath = fullPath,
            FileContent = await global::System.IO.File.ReadAllTextAsync(fullPath)
        };
    }
}
