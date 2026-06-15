namespace GeminiAgentKit.Types.System;

/// <summary>Role identifier constants used in <see cref="GeminiAgentKit.Types.Contents.Content"/> messages.</summary>
public record Role
{
    /// <summary>The system role, used for system instructions.</summary>
    public const string System = "system";

    /// <summary>The user role, used for human turns and function responses.</summary>
    public const string User = "user";

    /// <summary>The model role, used for assistant responses.</summary>
    public const string Model = "model";
}
