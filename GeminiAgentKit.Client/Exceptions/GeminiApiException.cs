namespace GeminiAgentKit.Client.Exceptions;

public class GeminiApiException : HttpRequestException
{
    public int StatusCode { get; }
    public string? Status { get; }

    public GeminiApiException(int statusCode, string message, string? status = null)
        : base(message, null, (System.Net.HttpStatusCode)statusCode)
    {
        StatusCode = statusCode;
        Status = status;
    }
}

internal sealed record GeminiErrorEnvelope(GeminiErrorDetail? Error);
internal sealed record GeminiErrorDetail(int Code, string Message, string? Status);
