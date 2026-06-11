namespace GeminiAgentKit.Utility;

public class Loader
{
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
