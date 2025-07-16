// Copyright (c) Ark -α- & Specyy.Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
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
        private readonly TimeSpan _initialCleanupPeriod;
        private readonly bool _adjustCleanupPeriod;

        private int _disposed;

        /// <summary>
        /// Creates a new <see cref="MemoryCache{TKey,T}"/> with an auto-adjusted cleanup period.
        /// </summary>
        public MemoryCache() : this(TimeSpan.FromMinutes(1))
        {
        }

        /// <summary>
        /// Creates a new <see cref="MemoryCache{TKey,T}"/> with an initial cleanup period of <see cref="Ini"/>
        /// </summary>
        /// <param name="initialCleanupPeriod">The initial cleanup period</param>
        /// <param name="adjustCleanupPeriod"></param>
        public MemoryCache(TimeSpan initialCleanupPeriod, bool adjustCleanupPeriod = true)
        {
            _initialCleanupPeriod = initialCleanupPeriod;
            _adjustCleanupPeriod = adjustCleanupPeriod;
            _cleanupTimer = new Timer(static state => ((MemoryCache<TKey, T>)state!).CleanupCallback(), this,
                initialCleanupPeriod, Timeout.InfiniteTimeSpan);
        }

        protected void CleanupCallback()
        {
            double avgLifetimeSecs = 0;
            int numEntries = 0;

            var cache = _cache;

            if (cache is null)
                return;

            // TODO: Loop in order of LRU
            foreach (var (key, entry) in cache)
            {
                if (_disposed != 0)
                    return;

                if ((!entry.IsExpired || !cache.TryRemove(new KeyValuePair<TKey, Entry>(key, entry))) && _adjustCleanupPeriod)
                {
                    // Prevent overflow
                    avgLifetimeSecs += Math.Min(entry.Lifetime.TotalSeconds, double.MaxValue - avgLifetimeSecs);
                    numEntries++;
                }
            }

            if (_adjustCleanupPeriod && _disposed == 0)
            {
                avgLifetimeSecs = numEntries == 0 ? 0 : avgLifetimeSecs / numEntries;

                Debug.Assert(avgLifetimeSecs >= 0);

                var newPeriod = GetNextCleanupPeriod(avgLifetimeSecs, numEntries);
                _cleanupTimer.Change(newPeriod, Timeout.InfiniteTimeSpan);
            }
        }

        protected virtual TimeSpan GetNextCleanupPeriod(double avgLifetimeSecs, int numEntries)
        {
            // Don't go overboard
            avgLifetimeSecs = Math.Max(1, avgLifetimeSecs != 0 ? avgLifetimeSecs : _initialCleanupPeriod.TotalSeconds);
            numEntries = Math.Max(1, numEntries);

            // Heuristic - can be adjusted later
            double periodSecs = avgLifetimeSecs + avgLifetimeSecs / numEntries;

            try
            {
                return TimeSpan.FromSeconds(periodSecs);
            }
            catch (OverflowException)
            {
                return TimeSpan.MaxValue;
            }
        }

        public bool ContainsKey(TKey key)
        {
            return _disposed == 0 && _cache.ContainsKey(key);
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

        public T AddOrUpdate<TArg>(TKey key,
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

            return _cache.AddOrUpdate(key, (k, arg) => new Entry(arg.AddFactory(k, arg.FactoryArgument)), (k, existing, arg) =>
            {
                var update = arg.UpdateFactory(k, new CacheEntry(existing.Item, existing.Lifetime, existing.LifetimeType), arg.FactoryArgument);
                return existing.Update(in update);
            }, state).Item;
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
                if (entry.IsExpired)
                {
                    _cache.TryRemove(new KeyValuePair<TKey, Entry>(key, entry));

                    item = null;
                    return false;
                }

                if (entry.LifetimeType == CacheLifetimeType.SlidingWindow)
                    entry.StartTime = DateTimeOffset.UtcNow;

                item = entry.Item;
                return true;
            }

            item = null;
            return false;
        }

        public bool TryGetEntry(TKey key, [NotNullWhen(true)] out CacheEntry? entry)
        {
            ThrowIfDisposed();

            if (_cache.TryGetValue(key, out var existing))
            {
                if (existing.IsExpired)
                {
                    _cache.TryRemove(new KeyValuePair<TKey, Entry>(key, existing));

                    entry = null;
                    return false;
                }

                if (existing.LifetimeType == CacheLifetimeType.SlidingWindow)
                    existing.StartTime = DateTimeOffset.UtcNow;

                entry = new CacheEntry(existing.Item, existing.Lifetime, existing.LifetimeType);
                return true;
            }

            entry = null;
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

        public void ExpireAll()
        {
            ThrowIfDisposed();
            _cache.Clear();
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

            // Allow any new entries racing with disposal to be garbage collected.
            // If we eventually allow subscribers to be notified on eviction, we may have to revisit this.
            _cache = null!;
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
                return;

            await _cleanupTimer.DisposeAsync().ConfigureAwait(false);
            _cache.Clear();

            // Allow any new entries racing with disposal to be garbage collected
            // If we eventually allow subscribers to be notified on eviction, we may have to revisit this.
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

        private class Entry : IEquatable<Entry>
        {
            public T Item { get; private set; }

            public TimeSpan Lifetime { get; private set; }

            public DateTimeOffset StartTime
            {
                get => _startTime;
                internal set
                {
                    Debug.Assert(value >= _startTime);

                    _version++;
                    _startTime = value;
                }
            }

            private DateTimeOffset _startTime;

            public CacheLifetimeType LifetimeType { get; private set; }
            public DateTimeOffset EndTime => StartTime.Add(Lifetime);
            public bool IsExpired => DateTimeOffset.UtcNow > EndTime;

            private long _version;

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
                Item = entry.Item;
                Lifetime = entry.Lifetime;
                LifetimeType = entry.LifetimeType;
                StartTime = DateTimeOffset.UtcNow;

                return this;
            }

            public override bool Equals(object? obj) => obj is Entry entry && Equals(entry);

            public bool Equals(Entry? other) => Item == other?.Item && _version == other._version;
        }
    }

    public enum CacheLifetimeType : byte
    {
        SlidingWindow,
        FixedWindow,
    }
}
