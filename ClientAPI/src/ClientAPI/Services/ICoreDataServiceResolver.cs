using ClientAPI.Models.Enums;
using ClientAPI.Services.Integrations;

namespace ClientAPI.Services;

public interface ICoreDataServiceResolver
{
    /// <summary>
    /// Returns the <see cref="ICoreDataServiceClient"/> mapped to the given <paramref name="osr"/> value
    /// according to the current configuration.
    /// </summary>
    ICoreDataServiceClient Resolve(Osr osr);
}
