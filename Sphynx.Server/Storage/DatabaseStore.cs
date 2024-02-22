using Sphynx.Server.Utils;

namespace Sphynx.Server.Storage
{
    /// <summary>
    /// An <see cref="ISphynxStore{TLookup,TData}"/> backed by a database.
    /// </summary>
    /// <typeparam name="TLookup">The lookup (key) type.</typeparam>
    /// <typeparam name="TData">The type of data which this <see cref="ISphynxStore{TLookup,TValue}"/> stores.</typeparam>
    public abstract class DatabaseStore<TLookup, TData> : IAsyncSphynxStore<TLookup, TData> where TLookup : notnull
    {
        /// <inheritdoc/>
        public abstract Task<SphynxErrorInfo<TData?>> GetValueAsync(TLookup key);
        
        /// <inheritdoc/>
        public abstract Task<bool> PutAsync(TLookup key, TData data);
        
        /// <inheritdoc/>
        public abstract Task<bool> DeleteAsync(TLookup key);
        
        /// <inheritdoc/>
        public SphynxErrorInfo<TData?> GetValue(TLookup key) => GetValueAsync(key).GetAwaiter().GetResult();

        /// <inheritdoc/>
        public bool Put(TLookup key, TData data) => PutAsync(key, data).GetAwaiter().GetResult();

        /// <inheritdoc/>
        public bool Delete(TLookup key) => DeleteAsync(key).GetAwaiter().GetResult();
    }
}