using APINexus.Models;

namespace APINexus.Services;

/// <summary>
/// Provides the list of API sources (internal or external) that can be rendered as
/// Scalar documents. Implementations are expected to be fast, synchronous, in-memory
/// reads — providers backed by remote/DB data should self-refresh via a background
/// cache rather than making this interface async.
/// </summary>
public interface IApiSourceProvider
{
    IReadOnlyList<ApiSourceDescriptor> GetApiSources();
}
