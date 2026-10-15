using System.Diagnostics;
using System.Text.RegularExpressions;
using GeminiAgentKit.Types.Attributes;

namespace CodeReviewAgent;

/// <summary>Read-only git tools the review agent can call. No shell is involved and refs are validated.</summary>
public partial class GitTools(string repoPath)
{
    private const int MaxOutputChars = 60_000;

    [GeminiFunction("List the files changed in the working tree. Pass baseRef (e.g. 'main') to compare against a branch, or staged=true for staged changes only.")]
    public Task<string> GetChangedFilesAsync(string? baseRef = null, bool staged = false)
        => RunGitAsync(BuildDiffArgs(["--name-status"], baseRef, staged));

    [GeminiFunction("Get the unified diff of the changes. Pass baseRef (e.g. 'main') to compare against a branch, staged=true for staged changes only, and path to limit the diff to one file.")]
    public Task<string> GetDiffAsync(string? baseRef = null, bool staged = false, string? path = null)
    {
        var args = BuildDiffArgs(["--unified=3"], baseRef, staged);
        if (!string.IsNullOrWhiteSpace(path))
        {
            args.Add("--");
            args.Add(path);
        }
        return RunGitAsync(args);
    }

    [GeminiFunction("Get the most recent commit messages (subject lines) to understand the intent of the change.")]
    public Task<string> GetRecentCommitsAsync(int count = 10)
        => RunGitAsync(["log", "--oneline", $"-{Math.Clamp(count, 1, 50)}"]);

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
        return output.Length <= MaxOutputChars
            ? output
            : output[..MaxOutputChars] + "\n... [truncated, ask for individual files with the path argument]";
    }

    [GeneratedRegex(@"^[\w./~^@{}-]+$")]
    private static partial Regex SafeRef();
}
