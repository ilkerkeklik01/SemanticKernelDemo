using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PizzaStore.API.Filters;

/// <summary>
/// Adds authorization role metadata to OpenAPI operations for assistant filtering.
/// </summary>
public class AuthorizeRolesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (AllowsAnonymous(context))
        {
            return;
        }

        var authorizeAttributes = GetAuthorizeAttributes(context).ToList();
        if (authorizeAttributes.Count == 0)
        {
            return;
        }

        var roles = authorizeAttributes
            .SelectMany(attribute => (attribute.Roles ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (roles.Count > 0)
        {
            var roleArray = new OpenApiArray();
            foreach (var role in roles)
            {
                roleArray.Add(new OpenApiString(role));
            }

            operation.Extensions["x-roles"] = roleArray;
        }
        else
        {
            operation.Extensions["x-roles"] = new OpenApiArray();
        }
    }

    private static bool AllowsAnonymous(OperationFilterContext context)
    {
        var methodAllows = context.MethodInfo.GetCustomAttributes(true)
            .OfType<AllowAnonymousAttribute>()
            .Any();

        var typeAllows = context.MethodInfo.DeclaringType?.GetCustomAttributes(true)
            .OfType<AllowAnonymousAttribute>()
            .Any() ?? false;

        return methodAllows || typeAllows;
    }

    private static IEnumerable<AuthorizeAttribute> GetAuthorizeAttributes(OperationFilterContext context)
    {
        var methodAttributes = context.MethodInfo.GetCustomAttributes(true)
            .OfType<AuthorizeAttribute>();

        var typeAttributes = context.MethodInfo.DeclaringType?.GetCustomAttributes(true)
            .OfType<AuthorizeAttribute>() ?? Enumerable.Empty<AuthorizeAttribute>();

        return methodAttributes.Concat(typeAttributes);
    }
}
