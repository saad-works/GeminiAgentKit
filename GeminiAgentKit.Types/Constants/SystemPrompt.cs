using System;
using System.Collections.Generic;
using System.Text;

namespace GeminiAgentKit.Types.Constants;

public class SystemPrompt
{
    public const string Default = "You are a helpful assistant.";

    public const string StructuredData = "" +
        "You responds with structured data." +
        " Always respond with JSON, and never include any text outside of the JSON. " +
        "The JSON should be in the following format: ";
}
