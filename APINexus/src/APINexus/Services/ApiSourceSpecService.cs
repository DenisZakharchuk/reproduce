using System.Net.Http.Json;
using System.Text.Json.Nodes;
using APINexus.Models;

namespace APINexus.Services;

internal sealed class ApiSourceSpecService(IApiSourceProvider sourceProvider, IHttpClientFactory httpClientFactory)
    : IApiSourceSpecService
{
    public async Task<ApiSourceSpecResult> GetRewrittenSpecAsync(
        string sourceId,
        IReadOnlyCollection<string> userRoles,
        CancellationToken cancellationToken)
    {
        var source = sourceProvider.GetApiSources().FirstOrDefault(s => s.Id == sourceId);
        if (source is null)
            return new ApiSourceSpecResult(ApiSourceSpecStatus.NotFound);

        var isAllowed = source.RequiredRoles.Count == 0 || source.RequiredRoles.Any(userRoles.Contains);
        if (!isAllowed)
            return new ApiSourceSpecResult(ApiSourceSpecStatus.Forbidden);

        var client = httpClientFactory.CreateClient("ApiSourceSpecFetcher");

        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync(source.SpecUrl, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new ApiSourceSpecResult(ApiSourceSpecStatus.UpstreamError);
        }

        if (!response.IsSuccessStatusCode)
            return new ApiSourceSpecResult(ApiSourceSpecStatus.UpstreamError);

        var document = await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken);
        if (document is null)
            return new ApiSourceSpecResult(ApiSourceSpecStatus.UpstreamError);

        if (source.Environments.Count > 0)
        {
            document["x-scalar-environments"] = BuildEnvironmentsNode(source.Environments);
            if (!string.IsNullOrEmpty(source.ActiveEnvironment))
                document["x-scalar-active-environment"] = source.ActiveEnvironment;
        }

        return new ApiSourceSpecResult(ApiSourceSpecStatus.Ok, document.ToJsonString());
    }

    private static JsonObject BuildEnvironmentsNode(
        IReadOnlyDictionary<string, ApiEnvironmentDescriptor> environments)
    {
        var node = new JsonObject();

        foreach (var (name, environment) in environments)
        {
            var environmentNode = new JsonObject();
            if (environment.Description is not null)
                environmentNode["description"] = environment.Description;
            if (environment.Color is not null)
                environmentNode["color"] = environment.Color;

            var variablesNode = new JsonObject();
            foreach (var (key, value) in environment.Variables)
                variablesNode[key] = value;
            environmentNode["variables"] = variablesNode;

            node[name] = environmentNode;
        }

        return node;
    }
}
