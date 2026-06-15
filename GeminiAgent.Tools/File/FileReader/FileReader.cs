using GeminiAgentKit.Types.Attributes;

namespace GeminiAgent.Tools.File.FileReader;

/// <summary>
/// Built-in tool that reads the full text content of a file by path.
/// Register with <c>client.AddTools&lt;FileReader&gt;()</c>.
/// </summary>
public class FileReader
{
    /// <summary>Reads all text from the file at <paramref name="fullPath"/> and returns its content.</summary>
    /// <param name="fullPath">The absolute path to the file to read.</param>
    /// <exception cref="FileNotFoundException">Thrown when no file exists at the specified path.</exception>
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
