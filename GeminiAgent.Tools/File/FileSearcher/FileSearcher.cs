using GeminiAgentKit.Client.Factories;
using GeminiAgentKit.Types.Tools;

namespace GeminiAgent.Tools.File.FileSearcher;

public class FileSearcher : IBaseTool
{
    private const string _toolName = "Search Files";
    private const string _toolDescription = "Search all files from a root directory or filter via search pattern";
    public static FunctionDeclaration GetTool()
    {
        return FunctionFactory.Create(_toolName, _toolDescription, SearchFilesAsync);
    }
    public static async Task<FileSearcherContext> SearchFilesAsync(string rootDirectory, string searchPattern = "*.*")
    {
        var files = new List<File>();
        if (!Directory.Exists(rootDirectory))
            throw new DirectoryNotFoundException($"The directory '{rootDirectory}' does not exist.");
        var filePaths = Directory.GetFiles(rootDirectory, searchPattern, SearchOption.AllDirectories);
        foreach (var path in filePaths)
        {
            var fileInfo = new FileInfo(path);
            files.Add(new File
            {
                Name = fileInfo.Name,
                Path = fileInfo.FullName,
                LastModified = fileInfo.LastWriteTime
            });
        }
        var allFiles = await Task.FromResult(files);

        return new FileSearcherContext
        {
            RootDirectory = rootDirectory,
            Files = allFiles
        };
    }
}
