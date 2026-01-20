// Import packages
using DotNetEnv;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SemanticKernelDemo2.Console;

// Load configuration
DotNetEnv.Env.TraversePath().Load();

// Populate values from your OpenAI deployment
var modelId = Environment.GetEnvironmentVariable("MODEL_ID") ?? string.Empty;
var endpoint = Environment.GetEnvironmentVariable("ENDPOINT") ?? string.Empty;
var apiKey = Environment.GetEnvironmentVariable("API_KEY") ?? string.Empty;

// Create a kernel with Azure OpenAI chat completion
var builder = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(modelId, endpoint, apiKey);

// Add enterprise components
builder.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Trace));

// Build the kernel
Kernel kernel = builder.Build();
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

// Add a plugin (the LightsPlugin class is defined below)
kernel.Plugins.AddFromType<LightsPlugin>("Lights");

// Enable planning
OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

// Create a history store the conversation
var history = new ChatHistory();

// Initiate a back-and-forth chat
string? userInput;
do
{
    // Collect user input
    Console.Write("User > ");
    userInput = Console.ReadLine();

    if (userInput is null)
    {
        continue;
    }

    // Add user input
    history.AddUserMessage(userInput);

    var response = chatCompletionService.GetStreamingChatMessageContentsAsync(
        history,
        openAIPromptExecutionSettings,
        kernel);
    string result = string.Empty;
     Console.WriteLine("Assistant > ");
    await foreach (var messagePart in response)
    {
        Console.Write(messagePart);
        result += messagePart;
    }
    Console.WriteLine();
    
    // Add the message from the agent to the chat history
    
    history.AddStreamingMessageAsync((IAsyncEnumerable<OpenAIStreamingChatMessageContent>)response, true);
    
} while (userInput?.ToLower() != "exit");