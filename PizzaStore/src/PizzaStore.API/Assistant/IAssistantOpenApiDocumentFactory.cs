using Microsoft.OpenApi.Models;

namespace PizzaStore.API.Assistant;

public interface IAssistantOpenApiDocumentFactory
{
    OpenApiDocument CreateDocument(HttpContext httpContext);
}
