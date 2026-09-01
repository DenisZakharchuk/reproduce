using System.Security.Claims;
using APINexus.Services;
using Scalar.AspNetCore;

namespace APINexus.Scalar;

/// <summary>
/// Composes the set of Scalar documents shown to the current user, fully hiding any
/// API source whose required roles the user does not hold.
/// </summary>
internal static class ScalarDocumentComposer
{
    internal static void Configure(
        ScalarOptions options,
        HttpContext httpContext,
        IApiSourceProvider sourceProvider,
        string? proxyUrl)
    {
        var userRoles = httpContext.User
            .FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var isFirst = true;
        foreach (var source in sourceProvider.GetApiSources())
        {
            var isAllowed = source.RequiredRoles.Count == 0 || source.RequiredRoles.Any(userRoles.Contains);
            if (!isAllowed)
                continue;

            // Sources with "Environments" configured are served through a local
            // rewriting endpoint that injects "x-scalar-environments" into the spec;
            // everything else is loaded straight from its original SpecUrl.
            var routePattern = source.RequiresSpecRewrite
                ? $"/api/sources/{source.Id}/openapi.json"
                : source.SpecUrl;

            options.AddDocument(source.Id, source.Title, routePattern, isDefault: isFirst);
            isFirst = false;
        }

        // Test requests against externally-hosted APIs are blocked by the browser's CORS
        // policy unless routed through a proxy. Opt-in via the "Scalar:ProxyUrl" config
        // setting; leave it unset to disable (e.g. when all sources are same-origin, or
        // when you don't want request/response bodies passing through a third-party proxy).
        if (!string.IsNullOrWhiteSpace(proxyUrl))
            options.WithProxy(proxyUrl);
    }
}
