using GeminiAgentKit.Types.Config;
using GeminiAgentKit.Types.System;
using Microsoft.Extensions.Configuration;

namespace GeminiAgentKit.Client.Builders;

/// <summary>
/// Loads <see cref="GeminiConfiguration"/> from <c>appsettings.json</c> and applies defaults.
/// </summary>
public class GeminiConfigurationBuilder
{
    /// <summary>
    /// Reads the <c>GeminiConfiguration</c> section from <c>appsettings.json</c> in the application's
    /// base directory and returns a populated <see cref="GeminiConfiguration"/> instance.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the configuration section is missing or empty.</exception>
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

        apiSettings.Model = Model.Gemma431B; //default model

        return apiSettings;
    }
}
