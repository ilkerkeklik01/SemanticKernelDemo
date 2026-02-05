using Microsoft.SemanticKernel;

namespace PizzaStore.API.Assistant;

public interface IAssistantKernelFactory
{
    Task<Kernel> CreateKernelAsync(HttpContext httpContext, CancellationToken cancellationToken);
}
