using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;

namespace OmniMart.API.Filters;

[AttributeUsage(AttributeTargets.Method)]
public class IdempotentAttribute : Attribute, IFilterFactory
{
    public bool IsReusable => false;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        var cache = serviceProvider.GetRequiredService<IDistributedCache>();
        return new IdempotencyFilter(cache);
    }
}


public class IdempotencyFilter : IAsyncActionFilter
{
    private readonly IDistributedCache _cache;
    private const string IdempotencyHeader = "X-Idempotency-Key";

    public IdempotencyFilter(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(IdempotencyHeader, out var idempotencyKey))
        {
            context.Result = new BadRequestObjectResult(new { Error = "Idempotency Key is missing in headers." });
            return;
        }

        var cacheKey = $"Idempotency_{idempotencyKey}";

        var isKeyExists = await _cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(isKeyExists))
        {
            context.Result = new ConflictObjectResult(new { Error = "This request has already been processed. Please wait." });
            return;
        }

        var executedContext = await next();

        if (executedContext.Exception == null)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) 
            };
            await _cache.SetStringAsync(cacheKey, "Processed", options);
        }
    }
}