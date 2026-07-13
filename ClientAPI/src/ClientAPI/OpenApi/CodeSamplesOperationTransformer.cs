using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using System.Text.Json.Nodes;

namespace ClientAPI.OpenApi;

internal sealed class CodeSamplesOperationTransformer(
    ICodeSampleProvider sampleProvider,
    IConfiguration configuration)
    : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        Microsoft.OpenApi.OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var baseUrl = ResolveBaseUrl(configuration);
        var metadata = OperationMetadataExtractor.Extract(context, baseUrl);
        var samples = sampleProvider.GetSamples(metadata);

        if (samples.Count == 0)
            return Task.CompletedTask;

        operation.Extensions ??= new Dictionary<string, IOpenApiExtension>();
        operation.Extensions["x-codeSamples"] = new JsonNodeExtension(
            new JsonArray([.. samples.Select(s => (JsonNode)new JsonObject
            {
                ["lang"]   = s.Lang,
                ["label"]  = s.Label,
                ["source"] = s.Source
            })]));

        return Task.CompletedTask;
    }

    private static string ResolveBaseUrl(IConfiguration config)
    {
        var urls = config["ASPNETCORE_URLS"] ?? config["urls"];
        return urls?.Split(';').First() ?? "http://localhost:5252";
    }
}
