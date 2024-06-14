namespace Sphynx.Storage
{
    /// <summary>
    /// Represents a generic data store in which items can be looked up/inserted by key and data.  
    /// </summary>
    /// <typeparam name="TLookup">The lookup (key) type.</typeparam>
    /// <typeparam name="TData">The type of data which this <see cref="ISphynxStore{TLookup,TValue}"/> stores.</typeparam>
    public interface ISphynxStore<in TLookup, TData> : IReadOnlySphynxStore<TLookup, TData> where TLookup : notnull
    {
        /// <summary>
        /// Places an item into this store, overriding a previous entry if necessary.
        /// </summary>
        /// <param name="index">The lookup key for the item.</param>
        /// <param name="data">The data associated with the lookup <paramref name="index"/>.</param>
        /// <returns>true if the item was successfully placed within this store; false otherwise.</returns>
        public bool Put(TLookup index, TData data);

        /// <summary>
        /// Deletes an item from this store.
        /// </summary>
        /// <param name="index">The lookup key associated with the item to delete.</param>
        /// <returns>true if the item was successfully found and deleted; false otherwise.</returns>
        public bool Delete(TLookup index);
    }

    /// <summary>
    /// Represents a generic, asynchronous data store in which items can be looked up/inserted by key and data.  
    /// </summary>
    /// <typeparam name="TLookup">The lookup (key) type.</typeparam>
    /// <typeparam name="TData">The type of data which this <see cref="ISphynxStore{TLookup,TValue}"/> stores.</typeparam>
    public interface IAsyncSphynxStore<in TLookup, TData> : ISphynxStore<TLookup, TData>, IAsyncReadOnlySphynxStore<TLookup, TData>
        where TLookup : notnull
    {
        /// <summary>
        /// Places an item into this store, overriding a previous entry if necessary.
        /// </summary>
        /// <param name="key">The lookup key for the item.</param>
        /// <param name="data">The data associated with the lookup <paramref name="key"/>.</param>
        /// <returns>true if the item was successfully placed within this store; false otherwise.</returns>
        public Task<bool> PutAsync(TLookup key, TData data);

        /// <summary>
        /// Deletes an item from this store.
        /// </summary>
        /// <param name="key">The lookup key associated with the item to delete.</param>
        /// <returns>true if the item was successfully found and deleted; false otherwise.</returns>
        public Task<bool> DeleteAsync(TLookup key);
    }

    /// <summary>
    /// Represents a generic data store in which items can be looked up by key. 
    /// </summary>
    /// <typeparam name="TLookup">The lookup (key) type.</typeparam>
    /// <typeparam name="TData">The type of data which this <see cref="ISphynxStore{TLookup,TValue}"/> stores.</typeparam>
    public interface IReadOnlySphynxStore<in TLookup, out TData> where TLookup : notnull
    {
        /// <summary>
        /// Retrieves data from this store associated with the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The lookup key for the item to retrieve.</param>
        /// <returns>The data which is associated with the specified <paramref name="index"/>.</returns>
        public TData? Get(TLookup index);
    }

    /// <summary>
    /// Represents a generic, asynchronous data store in which items can be looked up by key. 
    /// </summary>
    /// <typeparam name="TLookup">The lookup (key) type.</typeparam>
    /// <typeparam name="TData">The type of data which this <see cref="ISphynxStore{TLookup,TValue}"/> stores.</typeparam>
    public interface IAsyncReadOnlySphynxStore<in TLookup, TData> : IReadOnlySphynxStore<TLookup, TData> where TLookup : notnull
    {
        /// <summary>
        /// Retrieves data from this store associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The lookup key for the item to retrieve.</param>
        /// <returns>The data which is associated with the specified <paramref name="key"/>.</returns>
        public Task<TData?> GetAsync(TLookup key);
    }
}