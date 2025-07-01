// Copyright (c) Ark -α- & Specyy.Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics.CodeAnalysis;

namespace Sphynx.Storage
{
    /// <summary>
    /// A simple in-memory cache with periodic cleanup.
    /// </summary>
    /// <typeparam name="TKey">The type of key with which items will be queried.</typeparam>
    /// <typeparam name="T">The item type.</typeparam>
    public class MemoryCache<TKey, T> : IDisposable, IAsyncDisposable where TKey : notnull where T : class
    {
        private ShrinkingConcurrentDictionary<TKey, Entry> _cache = new();

        private readonly Timer _cleanupTimer;

        private int _disposed;

        public MemoryCache() : this(TimeSpan.FromMinutes(1))
        {
        }

        public MemoryCache(TimeSpan initialCleanupPeriod)
        {
            _cleanupTimer = new Timer(state =>
            {
                var thisRef = (MemoryCache<TKey, T>)state!;

                // TODO: Loop in order of LRU
                foreach (var (key, entry) in thisRef._cache)
                {
                    if (entry.IsExpired)
                        thisRef._cache.TryRemove(key, out _);
                }
            }, this, TimeSpan.Zero, initialCleanupPeriod);

            // TODO: Calc good heuristic for period
        }

        public void AddOrUpdate(TKey key, T item, TimeSpan lifetime, CacheLifetimeType lifetimeType = CacheLifetimeType.SlidingWindow)
        {
            ThrowIfDisposed();
            AddOrUpdate(key, new CacheEntry(item, lifetime, lifetimeType));
        }

        public void AddOrUpdate(TKey key, CacheEntry entry)
        {
            ThrowIfDisposed();
            _cache.AddOrUpdate(key, (_, arg) => new Entry(arg), (_, existing, arg) => existing.Update(in arg), factoryArgument: entry);
        }

        public void AddOrUpdate<TArg>(TKey key,
            Func<TKey, TArg, CacheEntry> addFactory,
            Func<TKey, CacheEntry, TArg, CacheEntry> updateFactory,
            TArg factoryArg)
        {
            ThrowIfDisposed();

            var state = new AddOrUpdateState<TArg>
            {
                AddFactory = addFactory,
                UpdateFactory = updateFactory,
                FactoryArgument = factoryArg,
            };

            _cache.AddOrUpdate(key, (k, arg) => new Entry(arg.AddFactory(k, arg.FactoryArgument)), (k, existing, arg) =>
            {
                var update = arg.UpdateFactory(k, new CacheEntry(existing.Item, existing.Lifetime, existing.LifetimeType), arg.FactoryArgument);
                return existing.Update(in update);
            }, state);
        }

        private readonly struct AddOrUpdateState<TArg>
        {
            public Func<TKey, TArg, CacheEntry> AddFactory { get; init; }
            public Func<TKey, CacheEntry, TArg, CacheEntry> UpdateFactory { get; init; }
            public TArg FactoryArgument { get; init; }
        }

        public bool TryAdd(TKey key, T item, TimeSpan lifetime, CacheLifetimeType lifetimeType = CacheLifetimeType.SlidingWindow)
        {
            ThrowIfDisposed();
            return TryAdd(key, new CacheEntry(item, lifetime, lifetimeType));
        }

        public bool TryAdd(TKey key, CacheEntry entry)
        {
            ThrowIfDisposed();
            return !_cache.ContainsKey(key) && _cache.TryAdd(key, new Entry(entry));
        }

        public bool TryGetItem(TKey key, [NotNullWhen(true)] out T? item)
        {
            ThrowIfDisposed();

            if (_cache.TryGetValue(key, out var entry))
            {
                if (entry.LifetimeType == CacheLifetimeType.SlidingWindow)
                {
                    entry.StartTime = DateTimeOffset.UtcNow;
                }
                else if (entry.IsExpired)
                {
                    _cache.TryRemove(new KeyValuePair<TKey, Entry>(key, entry));

                    item = null;
                    return false;
                }

                item = entry.Item;
                return true;
            }

            item = null;
            return false;
        }

        public bool TryGetEntry(TKey key, [NotNullWhen(true)] out CacheEntry? item)
        {
            ThrowIfDisposed();

            if (_cache.TryGetValue(key, out var entry))
            {
                if (entry.LifetimeType == CacheLifetimeType.SlidingWindow)
                {
                    entry.StartTime = DateTimeOffset.UtcNow;
                }
                else if (entry.IsExpired)
                {
                    _cache.TryRemove(new KeyValuePair<TKey, Entry>(key, entry));

                    item = null;
                    return false;
                }

                item = new CacheEntry(entry.Item, entry.Lifetime, entry.LifetimeType);
                return true;
            }

            item = null;
            return false;
        }

        public T GetOrAdd(TKey key, T item, TimeSpan lifetime, CacheLifetimeType lifetimeType = CacheLifetimeType.SlidingWindow)
        {
            ThrowIfDisposed();
            return GetOrAdd(key, new CacheEntry(item, lifetime, lifetimeType));
        }

        public T GetOrAdd(TKey key, CacheEntry entry)
        {
            ThrowIfDisposed();

            var existing = _cache.GetOrAdd(key, (_, arg) => new Entry(arg), factoryArgument: entry);
            return existing.Item;
        }

        public T GetOrAdd<TArg>(TKey key, Func<TKey, TArg, CacheEntry> valueFactory, TArg factoryArg)
        {
            ThrowIfDisposed();

            var state = new GetOrAddState<TArg>
            {
                ValueFactory = valueFactory,
                FactoryArgument = factoryArg,
            };

            var existing = _cache.GetOrAdd(key, (k, arg) => new Entry(arg.ValueFactory(k, arg.FactoryArgument)), state);
            return existing.Item;
        }

        private readonly struct GetOrAddState<TArg>
        {
            public Func<TKey, TArg, CacheEntry> ValueFactory { get; init; }
            public TArg FactoryArgument { get; init; }
        }

        public bool TryExpire(TKey key, [NotNullWhen(true)] out T? item)
        {
            ThrowIfDisposed();

            if (_cache.TryRemove(key, out var entry))
            {
                item = entry.Item;
                return true;
            }

            item = null;
            return false;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed != 0)
                throw new ObjectDisposedException(GetType().FullName);
        }

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
                return;

            _cleanupTimer.Dispose();
            _cache.Clear();

            // Allow any new entries racing with disposal to be garbage collected
            _cache = null!;
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
                return;

            await _cleanupTimer.DisposeAsync().ConfigureAwait(false);
            _cache.Clear();

            // Allow any new entries racing with disposal to be garbage collected
            _cache = null!;
        }

        public readonly record struct CacheEntry
        {
            public T Item { get; init; }
            public TimeSpan Lifetime { get; init; }
            public CacheLifetimeType LifetimeType { get; init; }

            public CacheEntry(T item, TimeSpan lifetime, CacheLifetimeType lifetimeType = CacheLifetimeType.SlidingWindow)
            {
                Item = item;
                Lifetime = lifetime;
                LifetimeType = lifetimeType;
            }
        }

        private class Entry
        {
            public T Item { get; private set; }

            public TimeSpan Lifetime { get; internal set; }
            public DateTimeOffset StartTime { get; internal set; }
            public CacheLifetimeType LifetimeType { get; internal set; }
            public DateTimeOffset EndTime => StartTime.Add(Lifetime);
            public bool IsExpired => DateTimeOffset.UtcNow > EndTime;

            public Entry(CacheEntry entry) : this(entry.Item, entry.Lifetime, entry.LifetimeType)
            {
            }

            public Entry(T item, TimeSpan lifetime, CacheLifetimeType lifetimeType)
            {
                Item = item;
                StartTime = DateTimeOffset.UtcNow;
                Lifetime = lifetime;
                LifetimeType = lifetimeType;
            }

            internal Entry Update(in CacheEntry entry)
            {
                Item = Item;
                Lifetime = entry.Lifetime;
                LifetimeType = entry.LifetimeType;
                StartTime = DateTimeOffset.UtcNow;

                return this;
            }
        }
    }

    public enum CacheLifetimeType
    {
        SlidingWindow,
        FixedWindow,
    }
}
