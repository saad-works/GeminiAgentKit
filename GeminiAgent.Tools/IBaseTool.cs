using GeminiAgentKit.Types.Tools;

namespace GeminiAgent.Tools;

public interface IBaseTool
{
    static abstract FunctionDeclaration GetTool();
}
