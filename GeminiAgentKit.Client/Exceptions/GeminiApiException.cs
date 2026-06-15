namespace GeminiAgentKit.Client.Exceptions;

/// <summary>
/// Thrown when the Gemini API returns a non-success HTTP status code.
/// Contains the numeric status code and an optional error status string from the response body.
/// </summary>
public class GeminiApiException : HttpRequestException
{
    /// <summary>The HTTP status code returned by the API.</summary>
    public new int StatusCode { get; }

    /// <summary>The error status string from the Gemini error response body, e.g. <c>INVALID_ARGUMENT</c>, or <see langword="null"/> if not provided.</summary>
    public string? Status { get; }

    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="message">Human-readable error message from the API response.</param>
    /// <param name="status">Optional machine-readable error status string.</param>
    public GeminiApiException(int statusCode, string message, string? status = null)
        : base(message, null, (System.Net.HttpStatusCode)statusCode)
    {
        StatusCode = statusCode;
        Status = status;
    }
}

internal sealed record GeminiErrorEnvelope(GeminiErrorDetail? Error);
internal sealed record GeminiErrorDetail(int Code, string Message, string? Status);
