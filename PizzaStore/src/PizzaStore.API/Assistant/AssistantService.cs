using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using PizzaStore.Core.CrossCuttingConcerns.Exceptions;

namespace PizzaStore.API.Assistant;

public class AssistantService : IAssistantService
{
    private const string SystemPrompt = "You are the PizzaStore assistant. Use the provided tools to answer questions and perform actions. Do not invent data. If a tool returns a 401 or 403, clearly explain that the user is not authorized for that action. If a tool returns a 404, say the resource was not found. Use only the tools provided.";

    private readonly IAssistantKernelFactory _kernelFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptions<AssistantOptions> _assistantOptions;
    private readonly ILogger<AssistantService> _logger;

    public AssistantService(
        IAssistantKernelFactory kernelFactory,
        IHttpContextAccessor httpContextAccessor,
        IOptions<AssistantOptions> assistantOptions,
        ILogger<AssistantService> logger)
    {
        _kernelFactory = kernelFactory;
        _httpContextAccessor = httpContextAccessor;
        _assistantOptions = assistantOptions;
        _logger = logger;
    }

    public async Task<AssistantChatResponse> ChatAsync(AssistantChatRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ValidationException("Message is required.");
        }

        var httpContext = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is not available.");
        var kernel = await _kernelFactory.CreateKernelAsync(httpContext, cancellationToken);
        var chatService = kernel.GetRequiredService<IChatCompletionService>();

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(SystemPrompt);
        AppendHistory(chatHistory, request.History);
        chatHistory.AddUserMessage(request.Message);

        var settings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            MaxTokens = _assistantOptions.Value.MaxTokens,
            Temperature = _assistantOptions.Value.Temperature
        };

        try
        {
            var response = await chatService.GetChatMessageContentAsync(
                chatHistory,
                executionSettings: settings,
                kernel: kernel,
                cancellationToken: cancellationToken);

            return new AssistantChatResponse
            {
                Message = response.Content ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Assistant chat failed.");
            return new AssistantChatResponse
            {
                Message = "Sorry, I couldn't complete that request. Please try again or rephrase."
            };
        }
    }

    private static void AppendHistory(ChatHistory chatHistory, List<AssistantChatMessage>? messages)
    {
        if (messages == null || messages.Count == 0)
        {
            return;
        }

        foreach (var message in messages)
        {
            if (string.IsNullOrWhiteSpace(message.Content))
            {
                continue;
            }

            var role = message.Role?.Trim().ToLowerInvariant();
            switch (role)
            {
                case "user":
                    chatHistory.AddUserMessage(message.Content);
                    break;
                case "assistant":
                    chatHistory.AddAssistantMessage(message.Content);
                    break;
                default:
                    // Ignore system/tool/unknown roles to prevent prompt injection.
                    break;
            }
        }
    }
}
