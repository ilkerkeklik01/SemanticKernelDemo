using AiUtilities.Application.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

#pragma warning disable SKEXP0070

namespace AiUtilities.Infrastructure.Ai;

public sealed class GeminiAiTextService : IAiTextService
{
    private readonly Kernel _kernel;
    private readonly KernelFunction _summarizeFunction;

    public GeminiAiTextService()
    {
        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                     ?? throw new InvalidOperationException("GEMINI_API_KEY not found");

        var builder = Kernel.CreateBuilder();

        builder.AddGoogleAIGeminiChatCompletion(
            modelId: "gemini-flash-latest",
            apiKey: apiKey
        );

        _kernel = builder.Build();

        var prompt = """
        Summarize the following text into {{$bulletCount}} concise bullet points.
        Use neutral, professional language.
        Do not add new information or opinions.
        If the text is short, keep bullets very concise.

        Text:
        {{$text}}
        """;

        _summarizeFunction = _kernel.CreateFunctionFromPrompt(
            prompt,
            functionName: "Summarize"
        );
    }

    public async Task<string> SummarizeAsync(string text, int bulletCount = 3)
    {
        var result = await _kernel.InvokeAsync(
            _summarizeFunction,
            new KernelArguments
            {
                ["text"] = text,
                ["bulletCount"] = bulletCount
            });

        return result.ToString();
    }
}
