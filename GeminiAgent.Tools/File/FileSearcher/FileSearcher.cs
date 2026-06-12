using GeminiAgentKit.Types.Attributes;

namespace GeminiAgent.Tools.File.FileSearcher;

public class FileSearcher
{
    [GeminiFunction("Search all files from a root directory or filter via search pattern")]
    public static async Task<FileSearcherContext> SearchFilesAsync(string rootDirectory, string searchPattern = "*.*")
    {
        if (!Directory.Exists(rootDirectory))
            throw new DirectoryNotFoundException($"The directory '{rootDirectory}' does not exist.");

        var filePaths = Directory.GetFiles(rootDirectory, searchPattern, SearchOption.AllDirectories);

        var files = new List<File>();
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

        return await Task.FromResult(new FileSearcherContext
        {
            RootDirectory = rootDirectory,
            Files = files
        });
    }
}
