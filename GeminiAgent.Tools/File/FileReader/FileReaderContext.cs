namespace GeminiAgent.Tools.File.FileReader;

/// <summary>The result returned by <see cref="FileReader"/> after reading a file.</summary>
public class FileReaderContext
{
    /// <summary>The file name without directory path.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The absolute path to the file that was read.</summary>
    public string FullPath { get; set; } = string.Empty;

    /// <summary>The full text content of the file.</summary>
    public string FileContent { get; set; } = string.Empty;
}
