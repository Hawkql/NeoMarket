using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Catching
{
    /// <summary>
    /// Базовый класс для Redis кэша.
    /// Паттерн: Cache-Aside. При любой ошибке Redis — логируем и пропускаем (graceful degradation).
    /// </summary>
    public abstract class RedisCacheBase(IDistributedCache cache, ILogger logger)
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        protected async Task<T?> GetAsync<T>(string key) where T : class
        {
            try
            {
                var data = await cache.GetStringAsync(key);
                return data is null ? null : JsonSerializer.Deserialize<T>(data, JsonOptions);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Redis GET failed for key '{Key}'", key);
                return null;
            }
        }

        protected async Task SetAsync<T>(string key, T value, TimeSpan ttl)
        {
            try
            {
                var json = JsonSerializer.Serialize(value, JsonOptions);
                await cache.SetStringAsync(key, json, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = ttl
                });
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Redis SET failed for key '{Key}'", key);
            }
        }

        protected async Task RemoveAsync(string key)
        {
            try
            {
                await cache.RemoveAsync(key);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Redis REMOVE failed for key '{Key}'", key);
            }
        }
    }
}
