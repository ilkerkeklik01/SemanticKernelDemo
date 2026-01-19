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
    private readonly KernelFunction _rewriteFunction;
    private readonly KernelFunction _classifyFunction;

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

        var rewritePrompt = """
        Rewrite the following text in a {{$tone}} tone.
        Preserve the meaning. Improve clarity. Do not add new facts.
        Return only the rewritten text.

        Text:
        {{$text}}
        """;

        _rewriteFunction = _kernel.CreateFunctionFromPrompt(
            rewritePrompt,
             functionName: "Rewrite"
             );

        var classifyPrompt = """
        Classify the following text into exactly ONE of these labels:
        Bug, Feature, Question, Other

        Rules:
        - Return ONLY the label word.
        - If unsure, return Other.

        Text:
        {{$text}}
        """;

        _classifyFunction = _kernel.CreateFunctionFromPrompt(classifyPrompt, functionName: "Classify");
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

    public async Task<string> RewriteAsync(string text, string tone = "professional")
    {
        var result = await _kernel.InvokeAsync(_rewriteFunction, new KernelArguments
        {
            ["text"] = text,
            ["tone"] = tone
        });

        return result.ToString();
    }

    public async Task<string> ClassifyAsync(string text)
    {
        var result = await _kernel.InvokeAsync(_classifyFunction, new KernelArguments
        {
            ["text"] = text
        });

        return result.ToString().Trim();
    }

}
