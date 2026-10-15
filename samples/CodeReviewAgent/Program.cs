using CodeReviewAgent;
using GeminiAgent.Tools.File.FileReader;
using GeminiAgentKit.Client;

// review: an AI code reviewer for your git changes, built on GeminiAgentKit.
//
//   review                  review uncommitted changes
//   review --staged         review only staged changes
//   review --base main      review everything since branching from main
//   review --dry-run        print the diff that would be reviewed (no API call)
//   review --repo <path>    review a repo other than the current directory
//
// Requires the GEMINI_API_KEY environment variable (https://aistudio.google.com/apikey).

var staged = args.Contains("--staged");
var dryRun = args.Contains("--dry-run");
var baseRef = ValueOf("--base");
var repo = Path.GetFullPath(ValueOf("--repo") ?? Directory.GetCurrentDirectory());

try
{
    await RunAsync();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"review failed: {(ex.InnerException ?? ex).Message}");
    Environment.ExitCode = 1;
}

async Task RunAsync()
{
    var git = new GitTools(repo);

    if (dryRun)
    {
        Console.WriteLine(await git.GetChangedFilesAsync(baseRef, staged));
        Console.WriteLine(await git.GetDiffAsync(baseRef, staged));
        return;
    }

    var client = new GeminiClient();
    client.AddTools(git);
    client.AddTools<FileReader>();

    var scope = baseRef is not null ? $"changes compared with '{baseRef}'"
              : staged ? "staged changes"
              : "uncommitted changes";

    var prompt = $"""
        You are a senior engineer doing a code review of the {scope} in the git repository at "{repo}".

        1. Call GetChangedFilesAsync, then GetDiffAsync (pass the same baseRef/staged options) to see what changed.
    2. When the diff is not enough to judge a change, read the full file with the file reader tool.
    3. Report only real problems: bugs, security issues, missing error handling, broken edge cases, and unclear code worth changing.
       Skip style nitpicks and do not praise the code.

    Format the answer as Markdown. For each finding give: severity (high / medium / low), file and line, what is wrong, and a concrete fix.
    If you find nothing worth reporting, say so in one line.
    """;

Console.Error.WriteLine($"Reviewing {scope} in {repo} ...");

var response = await client.GenerateContentAsync(prompt);
Console.WriteLine(response.Text);
}

string? ValueOf(string flag)
{
    var index = Array.IndexOf(args, flag);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}
