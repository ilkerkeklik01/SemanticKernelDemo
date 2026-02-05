using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Plugins.OpenApi;

namespace PizzaStore.API.Assistant;

public class AssistantKernelFactory : IAssistantKernelFactory
{
    private readonly IOptions<AzureOpenAIOptions> _openAiOptions;
    private readonly IAssistantOpenApiDocumentFactory _openApiDocumentFactory;

    public AssistantKernelFactory(
        IOptions<AzureOpenAIOptions> openAiOptions,
        IAssistantOpenApiDocumentFactory openApiDocumentFactory)
    {
        _openAiOptions = openAiOptions;
        _openApiDocumentFactory = openApiDocumentFactory;
    }

    public async Task<Kernel> CreateKernelAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        var options = _openAiOptions.Value;
        ValidateOptions(options);

        var builder = Kernel.CreateBuilder();
        builder.AddAzureOpenAIChatCompletion(
            deploymentName: options.Deployment,
            endpoint: options.Endpoint,
            apiKey: options.ApiKey);

        var kernel = builder.Build();

        var openApiDocument = _openApiDocumentFactory.CreateDocument(httpContext);
        using var openApiStream = SerializeDocument(openApiDocument);
        var httpClient = CreateHttpClient(httpContext, openApiDocument);

        #pragma warning disable SKEXP0040
        var executionParameters = new OpenApiFunctionExecutionParameters
        {
            HttpClient = httpClient
        };
        #pragma warning restore SKEXP0040

        var plugin = await OpenApiKernelPluginFactory.CreateFromOpenApiAsync(
            pluginName: "pizzastore",
            stream: openApiStream,
            executionParameters: executionParameters,
            cancellationToken: cancellationToken);

        kernel.Plugins.Add(plugin);
        return kernel;
    }

    private static void ValidateOptions(AzureOpenAIOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Endpoint))
            throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not configured.");
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException("AZURE_OPENAI_API_KEY is not configured.");
        if (string.IsNullOrWhiteSpace(options.Deployment))
            throw new InvalidOperationException("AZURE_OPENAI_DEPLOYMENT is not configured.");
    }

    private static MemoryStream SerializeDocument(OpenApiDocument document)
    {
        var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
        var jsonWriter = new OpenApiJsonWriter(writer);
        document.SerializeAsV3(jsonWriter);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }

    private static HttpClient CreateHttpClient(HttpContext httpContext, OpenApiDocument document)
    {
        var baseUrl = document.Servers.FirstOrDefault()?.Url;
        if (string.IsNullOrWhiteSpace(baseUrl) || !Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
        {
            throw new InvalidOperationException("OpenAPI server URL is missing or invalid.");
        }

        var innerHandler = new HttpClientHandler();
        var guardHandler = new AssistantApiHostGuardHandler(baseUri, innerHandler);
        var client = new HttpClient(guardHandler)
        {
            BaseAddress = baseUri,
            Timeout = TimeSpan.FromSeconds(30)
        };

        if (httpContext.Request.Headers.TryGetValue("Authorization", out var authHeader) && !string.IsNullOrWhiteSpace(authHeader))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authHeader.ToString());
        }

        return client;
    }
}
