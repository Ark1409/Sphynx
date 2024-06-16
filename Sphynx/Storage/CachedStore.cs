namespace Sphynx.Storage
{
    /// <summary>
    /// Represents an <see cref="ISphynxStore{TLookup,TData}"/> which caches its data. This class is not thread-safe.
    /// </summary>
    /// <typeparam name="TLookup">The lookup (key) type.</typeparam>
    /// <typeparam name="TData">The type of data which this <see cref="ISphynxStore{TLookup,TValue}"/> stores.</typeparam>
    public sealed class CachedStore<TLookup, TData> : ISphynxStore<TLookup, TData> where TLookup : notnull
    {
        /// <summary>
        /// Maximum number of times which an item from the cache can be retrieved before updating its value.
        /// </summary>
        public int CacheUpdateThreshold
        {
            get => _cacheUpdateThreshold;
            set
            {
                foreach (var (key, entry) in _cache)
                {
                    if (entry._cacheRetrievals > CacheUpdateThreshold)
                    {
                        entry._data = _innerStore.Get(key);
                        entry._cacheRetrievals = 0;
                    }
                }

                _cacheUpdateThreshold = value;
            }
        }

        private int _cacheUpdateThreshold;

        private readonly ISphynxStore<TLookup, TData> _innerStore;
        private readonly Dictionary<TLookup, CacheEntry<TData>> _cache;

        public CachedStore(ISphynxStore<TLookup, TData> innerStore, int cacheUpdateThreshold = 5)
        {
            _innerStore = innerStore;
            _cache = new Dictionary<TLookup, CacheEntry<TData>>();
            _cacheUpdateThreshold = cacheUpdateThreshold >= 0 ? cacheUpdateThreshold : throw new ArgumentException(nameof(cacheUpdateThreshold));
        }

        /// <inheritdoc/>
        public TData? Get(TLookup index)
        {
            if (_cache.TryGetValue(index, out var entry))
            {
                if (++entry._cacheRetrievals > CacheUpdateThreshold)
                {
                    entry._data = _innerStore.Get(index);
                    entry._cacheRetrievals = 0;
                }

                return entry._data;
            }

            if (!_cache.TryAdd(index, entry = new CacheEntry<TData>()))
            {
                return Get(index);
            }

            entry._data = _innerStore.Get(index);
            entry._cacheRetrievals = 0;
            return entry._data;
        }

        /// <inheritdoc cref="Put(TLookup,TData)"/>
        /// <param name="updateInnerStore">Whether to update the inner store backed by this cache.</param>
        public bool Put(TLookup key, TData data, bool updateInnerStore)
        {
            if (updateInnerStore && !_innerStore.Put(key, data))
                return false;

            if (!_cache.TryGetValue(key, out var entry) && !_cache.TryAdd(key, entry = new CacheEntry<TData>() { _data = data }))
            {
                return Put(key, data, updateInnerStore);
            }

            entry._data = data;
            return true;
        }

        /// <inheritdoc/>
        public bool Put(TLookup key, TData data) => Put(key, data, true);

        /// <summary>
        /// Clears the cache for this store.
        /// </summary>
        public void ClearCache() => _cache.Clear();

        /// <inheritdoc/>
        public bool Delete(TLookup index) => Delete(index, true);

        /// <inheritdoc cref="Delete(TLookup)"/>
        /// <param name="updateInnerStore">Whether to update the inner store backed by this cache.</param>
        public bool Delete(TLookup index, bool updateInnerStore) => (!updateInnerStore || _innerStore.Delete(index)) && _cache.Remove(index);

        private sealed class CacheEntry<T>
        {
            internal int _cacheRetrievals;
            internal T? _data;
        }
    }
}