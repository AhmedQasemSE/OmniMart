using MediatR;
using Microsoft.Extensions.Caching.Distributed; 
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Behaviors;

public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(IDistributedCache cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is ICacheableQuery cacheableQuery)
        {
            var cacheKey = cacheableQuery.CacheKey;

            var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);

            if (!string.IsNullOrEmpty(cachedData))
            {
                _logger.LogInformation("🎯 [Cache Hit] Fetched from Redis -> '{CacheKey}'", cacheKey);
                return JsonSerializer.Deserialize<TResponse>(cachedData)!;
            }

            _logger.LogInformation("⏳ [Cache Miss] Fetching from database -> '{CacheKey}'", cacheKey);
            var response = await next();

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = cacheableQuery.Expiration
            };

            var serializedData = JsonSerializer.Serialize(response);
            await _cache.SetStringAsync(cacheKey, serializedData, options, cancellationToken);

            return response;
        }

        return await next();
    }
}