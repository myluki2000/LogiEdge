using System;
using System.Collections.Generic;
using System.Text;
using LogiEdge.BaseService.MemoryCache;
using Microsoft.Extensions.Caching.Memory;

namespace LogiEdge.BaseService.Services
{
    public class MemoryCacheService(IMemoryCache cache)
    {
        public Guid CacheFile(CachedFile cachedFile, TimeSpan lifetime)
        {
            Guid id = Guid.NewGuid();

            cache.Set(id, cachedFile, lifetime);

            return id;
        }
    }
}
