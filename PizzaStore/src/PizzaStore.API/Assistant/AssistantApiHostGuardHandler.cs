using System.Net.Http;

namespace PizzaStore.API.Assistant;

public class AssistantApiHostGuardHandler : DelegatingHandler
{
    private readonly Uri _allowedBaseUri;

    public AssistantApiHostGuardHandler(Uri allowedBaseUri, HttpMessageHandler innerHandler)
        : base(innerHandler)
    {
        _allowedBaseUri = allowedBaseUri;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri == null)
        {
            throw new InvalidOperationException("Tool call blocked: request URI is missing.");
        }

        var resolvedUri = request.RequestUri.IsAbsoluteUri
            ? request.RequestUri
            : new Uri(_allowedBaseUri, request.RequestUri);

        if (!IsAllowed(resolvedUri))
        {
            throw new InvalidOperationException("Tool call blocked: target host is not allowed.");
        }

        request.RequestUri = resolvedUri;
        return base.SendAsync(request, cancellationToken);
    }

    private bool IsAllowed(Uri uri)
    {
        var allowedAuthority = _allowedBaseUri.GetLeftPart(UriPartial.Authority);
        var targetAuthority = uri.GetLeftPart(UriPartial.Authority);
        return string.Equals(allowedAuthority, targetAuthority, StringComparison.OrdinalIgnoreCase);
    }
}
