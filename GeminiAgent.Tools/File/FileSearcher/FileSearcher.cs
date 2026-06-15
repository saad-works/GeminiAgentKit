using GeminiAgentKit.Types.Attributes;

namespace GeminiAgent.Tools.File.FileSearcher;

/// <summary>
/// Built-in tool that recursively searches a directory for files matching an optional pattern.
/// Register with <c>client.AddTools&lt;FileSearcher&gt;()</c>.
/// </summary>
public class FileSearcher
{
    /// <summary>
    /// Recursively searches <paramref name="rootDirectory"/> for files matching <paramref name="searchPattern"/>
    /// and returns their names, paths, and last-modified timestamps.
    /// </summary>
    /// <param name="rootDirectory">The root directory to search.</param>
    /// <param name="searchPattern">Glob filter pattern, e.g. <c>*.cs</c>. Defaults to <c>*.*</c> (all files).</param>
    /// <exception cref="DirectoryNotFoundException">Thrown when <paramref name="rootDirectory"/> does not exist.</exception>
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
