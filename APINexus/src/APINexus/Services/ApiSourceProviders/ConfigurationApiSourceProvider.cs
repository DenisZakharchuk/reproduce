using APINexus.Configuration;
using APINexus.Models;
using Microsoft.Extensions.Options;

namespace APINexus.Services.ApiSourceProviders;

internal sealed class ConfigurationApiSourceProvider(IOptionsMonitor<ApiSourcesOptions> options)
    : IApiSourceProvider
{
    public IReadOnlyList<ApiSourceDescriptor> GetApiSources() =>
        options.CurrentValue.Sources
            .Select(s => new ApiSourceDescriptor(s.Id, s.Title, s.SpecUrl, s.Roles))
            .ToArray();
}
