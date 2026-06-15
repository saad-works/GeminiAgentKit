using System;
using System.Collections.Generic;
using System.Text;

namespace GeminiAgentKit.Types.Constants;

/// <summary>Built-in system prompt strings used as default instructions for the model.</summary>
public class SystemPrompt
{
    /// <summary>Default system instruction applied to every conversation.</summary>
    public const string Default = "You are a helpful assistant.";

    /// <summary>System instruction used when the model is expected to return only raw JSON.</summary>
    public const string StructuredData = "" +
        "You responds with structured data." +
        " Always respond with JSON, and never include any text outside of the JSON. " +
        "The JSON should be in the following format: ";
}
