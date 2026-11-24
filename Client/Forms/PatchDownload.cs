#nullable enable
using System.Net;

namespace Launcher;

// Only retry idempotent GETs and transient transport/status failures.
// Every retry reads a fresh response; partial data is never returned.
internal static class PatchDownload
{
    internal const int MaxAttempts = 4;

    internal static async Task<byte[]> FetchAsync(HttpClient client, Uri uri,
        Action<int>? onRetry = null, CancellationToken cancellationToken = default)
    {
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException("Patch host must use HTTP or HTTPS.", nameof(uri));

        for (int attempt = 1; ; attempt++)
        {
            try
            {
                // ResponseContentRead keeps the HTTP timeout in force until
                // the entire body has arrived, including slow or broken streams.
                using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseContentRead,
                    cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (attempt < MaxAttempts &&
                !cancellationToken.IsCancellationRequested && IsTransient(ex))
            {
                onRetry?.Invoke(attempt);
                await Task.Delay(TimeSpan.FromMilliseconds(500 * (1 << (attempt - 1))),
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static bool IsTransient(Exception error)
    {
        if (error is OperationCanceledException or IOException) return true;
        if (error is not HttpRequestException http) return false;
        if (http.StatusCode is { } status)
            return status == HttpStatusCode.RequestTimeout || (int)status == 429 || (int)status >= 500;
        // Certificate/authentication configuration errors must not be bypassed.
        return http.HttpRequestError is not (HttpRequestError.SecureConnectionError or
            HttpRequestError.UserAuthenticationError or HttpRequestError.ConfigurationLimitExceeded);
    }
}
