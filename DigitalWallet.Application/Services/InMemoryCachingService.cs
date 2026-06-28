using System.Collections.Concurrent;
using DigitalWallet.Application.Interfaces.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DigitalWallet.Infrastructure.Services
{
    /// <summary>
    /// In-memory caching implementation of ICachingService.
    /// Uses IMemoryCache — no Redis required.
    /// Note: cache is per-server. For multi-server deployments, switch back to Redis.
    /// </summary>
    public class InMemoryCachingService : ICachingService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<InMemoryCachingService> _logger;

        // Tracks every key we've stored so RemoveByPatternAsync can work
        // (IMemoryCache has no built-in key enumeration)
        private readonly ConcurrentDictionary<string, bool> _trackedKeys = new();

        private readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(5);

        public InMemoryCachingService(
            IMemoryCache cache,
            ILogger<InMemoryCachingService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        // ── GET ───────────────────────────────────────────────────────────

        public Task<T?> GetAsync<T>(string key)
        {
            _cache.TryGetValue(key, out T? value);
            return Task.FromResult(value);
        }

        // ── SET ───────────────────────────────────────────────────────────

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            var ttl = expiration ?? _defaultExpiration;

            var options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(ttl)
                // When the entry expires/evicts, remove it from our key tracker
                .RegisterPostEvictionCallback((evictedKey, _, _, _) =>
                {
                    _trackedKeys.TryRemove(evictedKey.ToString()!, out _);
                    _logger.LogDebug("Cache evicted: {Key}", evictedKey);
                });

            _cache.Set(key, value, options);
            _trackedKeys.TryAdd(key, true);

            _logger.LogDebug("Cache SET: {Key} (TTL: {TTL})", key, ttl);
            return Task.CompletedTask;
        }

        // ── GET OR SET ────────────────────────────────────────────────────

        public async Task<T?> GetOrSetAsync<T>(
            string key,
            Func<Task<T>> factory,
            TimeSpan? expiration = null)
        {
            // Return from cache if present
            if (_cache.TryGetValue(key, out T? cached))
            {
                _logger.LogDebug("Cache HIT:  {Key}", key);
                return cached;
            }

            _logger.LogDebug("Cache MISS: {Key}", key);

            // Call factory (hits the database)
            var value = await factory();

            if (value is not null)
            {
                await SetAsync(key, value, expiration);
            }

            return value;
        }

        // ── REMOVE ────────────────────────────────────────────────────────

        public Task RemoveAsync(string key)
        {
            _cache.Remove(key);
            _trackedKeys.TryRemove(key, out _);

            _logger.LogDebug("Cache DEL:  {Key}", key);
            return Task.CompletedTask;
        }

        // ── REMOVE BY PATTERN ─────────────────────────────────────────────

        /// <summary>
        /// Removes all keys matching a wildcard pattern, e.g. "transactions:wallet:123*"
        /// Supports trailing wildcards only (which is all this project uses).
        /// </summary>
        public Task RemoveByPatternAsync(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return Task.CompletedTask;

            // Strip trailing wildcard and do prefix match
            var prefix = pattern.TrimEnd('*');
            var isWildcard = pattern.EndsWith('*');

            var toRemove = isWildcard
                ? _trackedKeys.Keys
                    .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    .ToList()
                : _trackedKeys.Keys
                    .Where(k => k.Equals(pattern, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            foreach (var key in toRemove)
            {
                _cache.Remove(key);
                _trackedKeys.TryRemove(key, out _);
            }

            _logger.LogDebug("Cache DEL pattern: {Pattern} — removed {Count} key(s)",
                pattern, toRemove.Count);

            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string key)
        {
            var exists = _cache.TryGetValue(key, out _);
            return Task.FromResult(exists);
        }
    }
}