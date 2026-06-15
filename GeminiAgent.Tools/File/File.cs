namespace GeminiAgent.Tools.File;

/// <summary>Describes a single file entry returned by <see cref="FileSearcher.FileSearcher"/>.</summary>
public class File
{
    /// <summary>The file name without directory path.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The full absolute path to the file.</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>The date and time the file was last modified.</summary>
    public DateTime LastModified { get; set; }
}
