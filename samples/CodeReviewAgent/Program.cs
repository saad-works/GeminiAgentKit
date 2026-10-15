using CodeReviewAgent;
using GeminiAgentKit.Client;

// review: an AI code reviewer for your git changes, built on GeminiAgentKit.
//
//   review                    review uncommitted changes
//   review --staged           review only staged changes
//   review --base main        review everything changed compared with main
//   review --dry-run          print the diff that would be reviewed (no API call)
//   review --repo <path>      review a repo other than the current directory
//   review --model <id>       Gemini model to use (default: the kit default)
//   review --max-chars <n>    cap on diff characters returned per tool call (default 12000)
//
// Requires the GEMINI_API_KEY environment variable (https://aistudio.google.com/apikey).

var staged = args.Contains("--staged");
var dryRun = args.Contains("--dry-run");
var baseRef = ValueOf("--base");
var model = ValueOf("--model") ?? "";
var repo = Path.GetFullPath(ValueOf("--repo") ?? Directory.GetCurrentDirectory());
var maxChars = int.TryParse(ValueOf("--max-chars"), out var parsed) && parsed > 0 ? parsed : 12_000;

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
    var git = new GitTools(repo, maxChars);

    if (dryRun)
    {
        var files = await git.GetChangedFilesAsync(baseRef, staged);
        Console.WriteLine(files);
        foreach (var path in files.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.Split('\t').Last()))
            Console.WriteLine(await git.GetDiffAsync(baseRef, staged, path));
        return;
    }

    var client = new GeminiClient();
    client.AddTools(git);

    var scope = baseRef is not null ? $"changes compared with '{baseRef}'"
              : staged ? "staged changes"
              : "uncommitted changes";

    var options = $"baseRef={(baseRef is null ? "null" : $"\"{baseRef}\"")}, staged={staged.ToString().ToLowerInvariant()}";

    var prompt = $"""
        You are a senior engineer doing a code review of the {scope} in the git repository at "{repo}".

        Work file by file so each step stays small:
        1. Call GetChangedFilesAsync with {options}.
        2. For each changed source file, call GetDiffAsync with {options} and path set to that file. Skip binary, generated and lock files.
        3. Only read a whole file with ReadFileAsync when the diff alone is not enough to judge a change.
        4. Report only real problems: bugs, security issues, missing error handling, broken edge cases, and code that is unclear enough to be worth changing.
           Skip style nitpicks and do not praise the code.

        Format the answer as Markdown. For each finding give: severity (high / medium / low), file and line, what is wrong, and a concrete fix.
        If you find nothing worth reporting, say so in one line.
        """;

    Console.Error.WriteLine($"Reviewing {scope} in {repo} ...");

    var response = await client.GenerateContentAsync(model, prompt);
    Console.WriteLine(response.Text);
}

string? ValueOf(string flag)
{
    var index = Array.IndexOf(args, flag);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}
