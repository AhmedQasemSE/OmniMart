using System;

namespace OmniMart.Application.Common;

public interface ICacheableQuery
{
    string CacheKey { get; }
    TimeSpan Expiration { get; }
}