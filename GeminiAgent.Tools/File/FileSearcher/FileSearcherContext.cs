namespace GeminiAgent.Tools.File.FileSearcher;

/// <summary>The result returned by <see cref="FileSearcher"/> after searching a directory.</summary>
public class FileSearcherContext
{
    /// <summary>The root directory that was searched.</summary>
    public string RootDirectory { get; set; } = string.Empty;

    /// <summary>All files found under <see cref="RootDirectory"/> matching the search pattern.</summary>
    public List<File> Files { get; set; } = new();
}
