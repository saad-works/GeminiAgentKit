using GeminiAgentKit.Types.Config;
using Microsoft.Extensions.Configuration;

namespace GeminiAgentKit.Client.Builders;

public class GeminiConfigurationBuilder
{
    public static GeminiConfiguration Build()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var apiSettings = configuration
            .GetSection(nameof(GeminiConfiguration))
            .Get<GeminiConfiguration>();
        
        if(apiSettings == null)
            throw new InvalidOperationException("Failed to load GeminiConfiguration from appsettings.json.");

        return apiSettings;
    }
}
