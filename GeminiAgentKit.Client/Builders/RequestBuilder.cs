using GeminiAgentKit.Types.Configurations;
using GeminiAgentKit.Types.Constants;
using GeminiAgentKit.Types.Contents;
using GeminiAgentKit.Types.Requests;
using GeminiAgentKit.Types.Schema;
using GeminiAgentKit.Types.System;
using GeminiAgentKit.Types.Tools;

namespace GeminiAgentKit.Client.Builders;

public static class RequestBuilder
{
    public static GenerateContentRequest Build(
        string instructions,
        List<FunctionDeclaration> tools,
        GenerateContentRequest? request = null)
    {
        if (request == null)
        {
            request = new();
            request.SystemInstruction = new Content { Parts = [new Part { Text = SystemPrompt.Default }], Role = Role.User };
            if (tools.Count > 0)
                request.Tools.Add(new Tool { FunctionDeclarations = tools });
        }

        request.Contents.Add(new Content { Parts = [new Part { Text = instructions }], Role = Role.User });

        return request;
    }

    public static GenerateContentRequest Build<T>(
        string instructions,
        List<FunctionDeclaration> tools,
        GenerateContentRequest? request = null)
    {
        request = Build(instructions, tools, request);

        request.GenerationConfig = new GenerationConfig
        {
            ResponseMimeType = ResponseType.Json,
            ResponseSchema   = GeminiSchemaBuilder.Build<T>()
        };

        return request;
    }
}
