using APINexus.Configuration;
using APINexus.Models;
using Microsoft.Extensions.Options;

namespace APINexus.Services.ApiSourceProviders;

internal sealed class ConfigurationApiSourceProvider(IOptionsMonitor<ApiSourcesOptions> options)
    : IApiSourceProvider
{
    public IReadOnlyList<ApiSourceDescriptor> GetApiSources() =>
        options.CurrentValue.Sources
            .Select(s => new ApiSourceDescriptor(
                s.Id,
                s.Title,
                s.SpecUrl,
                s.Roles,
                s.Environments.ToDictionary(
                    e => e.Key,
                    e => new ApiEnvironmentDescriptor(e.Value.Description, e.Value.Color, e.Value.Variables)),
                s.ActiveEnvironment))
            .ToArray();
}
