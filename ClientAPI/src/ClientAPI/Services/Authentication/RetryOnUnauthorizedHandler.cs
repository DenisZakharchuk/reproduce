using System.Net;

namespace ClientAPI.Services.Authentication;

/// <summary>
/// Outer handler: if the authenticated request comes back 401, it re-runs the
/// inner pipeline (which re-authenticates) and retries the request once.
/// Registered before <see cref="AuthenticationHandler"/> so it wraps it.
/// </summary>
public class RetryOnUnauthorizedHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Buffer a copy up front — a request that has been sent cannot be resent.
        var retryRequest = await CloneAsync(request, cancellationToken);

        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        response.Dispose();

        // Tell the authenticator to get a fresh token before the second attempt.
        retryRequest.Options.Set(AuthenticationHttpRequestOptions.ForceReauthentication, true);
        return await base.SendAsync(retryRequest, cancellationToken);
    }

    private static async Task<HttpRequestMessage> CloneAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };

        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        foreach (var option in (IDictionary<string, object?>)request.Options)
            ((IDictionary<string, object?>)clone.Options)[option.Key] = option.Value;

        if (request.Content is not null)
        {
            var buffer = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            var content = new ByteArrayContent(buffer);
            foreach (var header in request.Content.Headers)
                content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            clone.Content = content;
        }

        return clone;
    }
}
