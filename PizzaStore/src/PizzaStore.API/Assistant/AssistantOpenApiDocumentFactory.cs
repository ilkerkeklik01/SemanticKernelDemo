using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;

namespace PizzaStore.API.Assistant;

public class AssistantOpenApiDocumentFactory : IAssistantOpenApiDocumentFactory
{
    private readonly ISwaggerProvider _swaggerProvider;
    private readonly IOptions<AssistantOptions> _assistantOptions;

    public AssistantOpenApiDocumentFactory(ISwaggerProvider swaggerProvider, IOptions<AssistantOptions> assistantOptions)
    {
        _swaggerProvider = swaggerProvider;
        _assistantOptions = assistantOptions;
    }

    public OpenApiDocument CreateDocument(HttpContext httpContext)
    {
        var document = _swaggerProvider.GetSwagger("v1");
        var isAdmin = httpContext.User.IsInRole("Admin");
        var baseUri = ResolveBaseUri(httpContext);

        document.Servers = new List<OpenApiServer>
        {
            new OpenApiServer { Url = baseUri.ToString().TrimEnd('/') }
        };

        FilterPaths(document, isAdmin);
        return document;
    }

    private void FilterPaths(OpenApiDocument document, bool isAdmin)
    {
        foreach (var path in document.Paths.ToList())
        {
            if (ShouldExcludePath(path.Key))
            {
                document.Paths.Remove(path.Key);
                continue;
            }

            var operations = path.Value.Operations.ToList();
            foreach (var operation in operations)
            {
                if (!isAdmin && RequiresAdmin(operation.Value))
                {
                    path.Value.Operations.Remove(operation.Key);
                }
            }

            if (path.Value.Operations.Count == 0)
            {
                document.Paths.Remove(path.Key);
            }
        }
    }

    private static bool ShouldExcludePath(string path)
    {
        return path.StartsWith("/api/assistant", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase);
    }

    private static bool RequiresAdmin(OpenApiOperation operation)
    {
        if (operation.Extensions.TryGetValue("x-roles", out var rolesValue) && rolesValue is OpenApiArray rolesArray)
        {
            return rolesArray
                .OfType<OpenApiString>()
                .Any(role => string.Equals(role.Value, "Admin", StringComparison.OrdinalIgnoreCase));
        }

        return false;
    }

    private Uri ResolveBaseUri(HttpContext httpContext)
    {
        var configured = _assistantOptions.Value.ApiBaseUrl;
        if (!string.IsNullOrWhiteSpace(configured))
        {
            if (!Uri.TryCreate(configured, UriKind.Absolute, out var configuredUri))
            {
                throw new InvalidOperationException("ASSISTANT_API_BASE_URL must be an absolute URL.");
            }

            return configuredUri;
        }

        var request = httpContext.Request;
        var scheme = request.Scheme;
        var host = request.Host.Value;
        var pathBase = request.PathBase.HasValue ? request.PathBase.Value : string.Empty;

        var baseUrl = $"{scheme}://{host}{pathBase}";
        return new Uri(baseUrl);
    }
}
