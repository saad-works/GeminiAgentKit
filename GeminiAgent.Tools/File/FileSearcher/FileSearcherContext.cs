namespace GeminiAgent.Tools.File.FileSearcher;

public class FileSearcherContext
{
    public string RootDirectory { get; set; } = string.Empty;
    public List<File> Files { get; set; } = new();
}

