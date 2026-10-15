# CodeReviewAgent

An AI code reviewer for your git changes, in about 120 lines of C#, built on GeminiAgentKit.

The agent gets three read-only git tools plus the built-in `FileReader`. It lists what changed, reads the diff, opens whole files when the diff is not enough, and reports real problems with severity, location and a suggested fix.

```bash
export GEMINI_API_KEY="your-key"     # PowerShell: $env:GEMINI_API_KEY = "your-key"

dotnet run --project samples/CodeReviewAgent -- --repo /path/to/your/repo
```

| Flag | Meaning |
|---|---|
| *(none)* | Review uncommitted changes |
| `--staged` | Review only staged changes |
| `--base main` | Review everything changed compared with a branch or ref |
| `--repo <path>` | Repository to review (default: current directory) |
| `--dry-run` | Print the diff that would be reviewed, with no API call |

## How it uses the kit

```csharp
var client = new GeminiClient();
client.AddTools(new GitTools(repo));   // [GeminiFunction] methods: changed files, diff, recent commits
client.AddTools<FileReader>();         // built-in tool

var response = await client.GenerateContentAsync(prompt);
```

The model decides which tools to call and in what order. `GeminiAgentKit` runs them and feeds the results back until the review is done. See `GitTools.cs` for how little code a tool takes.

## Safety

- Tools are read-only: `git diff` and `git log` only. No shell is involved (arguments are passed as a list), and refs are validated so a model cannot inject git options.
- `FileReader` can read any file your user can. Run the reviewer on code you are comfortable sending to the Gemini API, since diffs and file contents are sent to Google.
