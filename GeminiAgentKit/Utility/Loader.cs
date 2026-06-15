namespace GeminiAgentKit.Utility;

/// <summary>Displays an animated spinner in the console while an async operation is in progress.</summary>
public class Loader
{
    /// <summary>
    /// Renders a rotating spinner on the current console line until <paramref name="task"/> completes,
    /// then clears the line.
    /// </summary>
    /// <param name="task">The operation to wait for.</param>
    public static async Task Invoke(Task task)
    {
        var spinner = new[] { '|', '/', '-', '\\' };
        int i = 0;

        while (!task.IsCompleted)
        {
            Console.Write($"\rLoading... {spinner[i++ % spinner.Length]}");
            await Task.Delay(100);
        }

        Console.Write("\r                    \r");
    }
}
