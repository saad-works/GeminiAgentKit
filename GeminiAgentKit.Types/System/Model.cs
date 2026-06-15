namespace GeminiAgentKit.Types.System;

/// <summary>Model identifier constants for the Gemini API.</summary>
public record Model
{
    /// <summary>Gemini 3.1 Flash Lite — fast, lightweight model.</summary>
    public const string Gemini31FlashLite = "gemini-3.1-flash-lite";

    /// <summary>Gemini 3.5 Flash — balanced speed and capability.</summary>
    public const string Gemini35Flash = "gemini-3.5-flash";

    /// <summary>Gemini 2.5 Flash — latest flash-tier model.</summary>
    public const string Gemini25Flash = "gemini-2.5-flash";

    /// <summary>Gemini 2.5 Pro — highest capability model.</summary>
    public const string Gemini25Pro = "gemini-2.5-pro";

    /// <summary>Gemma 4 31B instruction-tuned open model.</summary>
    public const string Gemma431B = "gemma-4-31b-it";
}
