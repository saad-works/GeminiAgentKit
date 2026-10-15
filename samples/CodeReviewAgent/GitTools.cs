using System.Diagnostics;
using System.Text.RegularExpressions;
using GeminiAgentKit.Types.Attributes;

namespace CodeReviewAgent;

/// <summary>Read-only git tools the review agent can call. No shell is involved and refs are validated.</summary>
public partial class GitTools(string repoPath, int maxChars = 12_000)
{
    // Files that are large, generated or not worth reviewing; excluded from the changed-file list and from diffs.
    private static readonly string[] ExcludedPaths =
    [
        "package-lock.json", "yarn.lock", "pnpm-lock.yaml", "*.lock", "*.min.js", "*.min.css", "*.map",
        "*.png", "*.jpg", "*.jpeg", "*.gif", "*.ico", "*.pdf", "*.vsix", "*.nupkg", "*.dll", "*.exe",
        "dist/**", "bin/**", "obj/**", "node_modules/**"
    ];

    [GeminiFunction("List the files changed in the working tree (generated, binary and lock files are left out). Pass baseRef (e.g. 'main') to compare against a branch, or staged=true for staged changes only.")]
    public Task<string> GetChangedFilesAsync(string? baseRef = null, bool staged = false)
    {
        var args = BuildDiffArgs(["--name-status"], baseRef, staged);
        AddPathspec(args, null);
        return RunGitAsync(args);
    }

    [GeminiFunction("Get the unified diff of one changed file. Pass baseRef (e.g. 'main') to compare against a branch, staged=true for staged changes only, and path for the file. Large diffs are truncated.")]
    public Task<string> GetDiffAsync(string? baseRef = null, bool staged = false, string? path = null)
    {
        var args = BuildDiffArgs(["--unified=3"], baseRef, staged);
        AddPathspec(args, path);
        return RunGitAsync(args);
    }

    [GeminiFunction("Read the full text of a file in the repository. path is relative to the repository root.")]
    public async Task<string> ReadFileAsync(string path)
    {
        var root = Path.GetFullPath(repoPath);
        var full = Path.GetFullPath(Path.Combine(root, path));

        if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"'{path}' is outside the repository.");
        if (!File.Exists(full))
            throw new FileNotFoundException($"'{path}' does not exist in the repository.");

        var text = await File.ReadAllTextAsync(full);
        return text.Length <= maxChars * 3 ? text : text[..(maxChars * 3)] + "\n... [file truncated]";
    }

    [GeminiFunction("Get the most recent commit messages (subject lines) to understand the intent of the change.")]
    public Task<string> GetRecentCommitsAsync(int count = 10)
        => RunGitAsync(["log", "--oneline", $"-{Math.Clamp(count, 1, 50)}"]);

    private static void AddPathspec(List<string> args, string? path)
    {
        args.Add("--");
        args.Add(string.IsNullOrWhiteSpace(path) ? "." : path);
        args.AddRange(ExcludedPaths.Select(p => $":(exclude,glob)**/{p}"));
    }

    private static List<string> BuildDiffArgs(IEnumerable<string> extra, string? baseRef, bool staged)
    {
        var args = new List<string> { "diff" };
        args.AddRange(extra);

        if (staged) args.Add("--staged");

        if (!string.IsNullOrWhiteSpace(baseRef))
        {
            if (!SafeRef().IsMatch(baseRef) || baseRef.StartsWith('-'))
                throw new ArgumentException($"'{baseRef}' is not a valid git ref.");
            args.Add(baseRef);
        }

        return args;
    }

    private async Task<string> RunGitAsync(List<string> args)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = repoPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var arg in args) startInfo.ArgumentList.Add(arg);

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start git.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"git {string.Join(' ', args)} failed: {await stderr}");

        var output = await stdout;
        return output.Length <= maxChars
            ? output
            : output[..maxChars] + "\n... [diff truncated; review the rest by reading the file]";
    }

    [GeneratedRegex(@"^[\w./~^@{}-]+$")]
    private static partial Regex SafeRef();
}
