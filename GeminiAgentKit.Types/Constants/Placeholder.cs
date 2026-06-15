using System;
using System.Collections.Generic;
using System.Text;

namespace GeminiAgentKit.Types.Constants;

/// <summary>Template placeholder strings substituted at runtime when building API URLs.</summary>
public class Placeholder
{
    /// <summary>Placeholder replaced with the model identifier in the base URL template.</summary>
    public const string MODEL = "{{model}}";
}
