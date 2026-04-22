using System.Threading.RateLimiting;

namespace ProzorroAnalytics.Infrastructure.Http;

public sealed class RateLimitingHandler : DelegatingHandler
{
    private readonly RateLimiter _rateLimiter;

    public RateLimitingHandler(RateLimiter rateLimiter)
    {
        _rateLimiter = rateLimiter;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        using var lease = await _rateLimiter.AcquireAsync(permitCount: 1, cancellationToken);

        if (!lease.IsAcquired)
            throw new InvalidOperationException("Rate limit exceeded.");

        return await base.SendAsync(request, cancellationToken);
    }
}
