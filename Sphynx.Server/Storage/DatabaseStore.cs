using System.Reflection;
using Sphynx.Utils;
using Sphynx.Storage;

namespace Sphynx.Server.Storage
{
    /// <summary>
    /// An <see cref="ISphynxStore{TLookup,TData}"/> backed by a database.
    /// </summary>
    /// <typeparam name="TIndex">Identifier used to access a single data entity within the store, such as a primary key
    /// for a MySQL table row or a single document ID in MongoDB.</typeparam>
    /// <typeparam name="TContent">The type of data which this <see cref="DatabaseStore{TLookup,TValue}"/> stores at
    /// <see cref="TIndex"/>. This would typically be a type describing a MySQL table row or MongoDB document.</typeparam>
    /// <remarks>A single <see cref="DatabaseStore{TLookup, TValue}"/> should represent a single storage system within
    /// a database, such as a MySQL table or a MongoDB collection.</remarks>
    public abstract class DatabaseStore<TIndex, TContent> : IAsyncReadOnlySphynxStore<TIndex, SphynxErrorInfo<TContent?>>,
        IAsyncSphynxStore<TIndex, TContent?> where TIndex : notnull
    {
        /// <inheritdoc/>
        public abstract Task<bool> PutAsync(TIndex key, TContent? data);
        
        /// <inheritdoc/>
        public abstract Task<SphynxErrorInfo<TContent?>> GetAsync(TIndex key);

        /// <inheritdoc/>
        public abstract Task<bool> DeleteAsync(TIndex key);

        /// <summary>
        /// Upserts the value of a single field within a data entity held inside this store.
        /// </summary>
        /// <param name="index">The <see cref="TIndex"/> in which the <paramref name="fieldName"/> is located.</param>
        /// <param name="fieldName">The name of the field to update.</param>
        /// <param name="value">The value to the field.</param>
        /// <typeparam name="TValue">The data type of <paramref name="value"/>.</typeparam>
        /// <returns>true if the operation completed successfully; false otherwise.</returns>
        public abstract Task<bool> PutFieldAsync<TValue>(TIndex index, string fieldName, TValue? value);

        /// <summary>
        /// Upserts the value of a single field within a data entity held inside this store.
        /// </summary>
        /// <param name="index">The <see cref="TIndex"/> in which the <paramref name="field"/> is located.</param>
        /// <param name="field">The field to update.</param>
        /// <param name="value">The value to the field.</param>
        /// <typeparam name="TValue">The data type of <paramref name="value"/>.</typeparam>
        /// <returns>true if the operation completed successfully; false otherwise.</returns>
        public virtual Task<bool> PutFieldAsync<TValue>(TIndex index, PropertyInfo field, TValue? value) => PutFieldAsync(index, field.Name, value);

        /// <summary>
        /// Retrieves the value of a single field within a data entity held inside this store.
        /// </summary>
        /// <param name="index">The <see cref="TIndex"/> in which the <paramref name="fieldName"/> is located.</param>
        /// <param name="fieldName">The name of the field value to retrieve.</param>
        /// <typeparam name="TValue">The data type of the field.</typeparam>
        /// <returns>The value to the field.</returns>
        public abstract Task<SphynxErrorInfo<TValue?>> GetFieldAsync<TValue>(TIndex index, string fieldName);
        
        /// <summary>
        /// Checks whether this store has <see cref="TContent"/> which contains a field with the
        /// specified <see cref="fieldName"/>.
        /// </summary>
        /// <param name="fieldName">The name of the field to check.</param>
        /// <returns>true if there is <see cref="TContent"/> which contains a field with the specified
        /// name; false otherwise.</returns>
        public abstract Task<bool> ContainsFieldAsync(string fieldName);

        /// <inheritdoc/>
        public bool Put(TIndex key, TContent? data) => PutAsync(key, data).GetAwaiter().GetResult();
        
        /// <inheritdoc/>
        public SphynxErrorInfo<TContent?> Get(TIndex key) => GetAsync(key).GetAwaiter().GetResult();

        /// <inheritdoc cref="PutFieldAsync{TValue}(TIndex,PropertyInfo,TValue?)"/>
        public bool PutField<TValue>(TIndex index, PropertyInfo field, TValue? value) =>
            PutFieldAsync(index, field, value).GetAwaiter().GetResult();

        /// <inheritdoc cref="PutFieldAsync{TValue}(TIndex,string,TValue?)"/>
        public bool PutField<TValue>(TIndex index, string fieldName, TValue? value) =>
            PutFieldAsync(index, fieldName, value).GetAwaiter().GetResult();

        /// <inheritdoc cref="GetFieldAsync{TValue}(TIndex,string)"/>
        public SphynxErrorInfo<TValue?> GetField<TValue>(TIndex index, string fieldName) =>
            GetFieldAsync<TValue>(index, fieldName).GetAwaiter().GetResult();

        /// <inheritdoc cref="ContainsFieldAsync"/>
        public bool ContainsField(string fieldName) => ContainsFieldAsync(fieldName).GetAwaiter().GetResult();
        
        /// <inheritdoc/>
        public bool Delete(TIndex key) => DeleteAsync(key).GetAwaiter().GetResult();

        /// <inheritdoc/>
        TContent? IReadOnlySphynxStore<TIndex, TContent?>.Get(TIndex key) => Get(key).Data;

        /// <inheritdoc/>
        async Task<TContent?> IAsyncReadOnlySphynxStore<TIndex, TContent?>.GetAsync(TIndex key) => (await GetAsync(key)).Data;
    }

    /// <summary>
    /// Holds name of file which stores database information.
    /// </summary>
    public static class DatabaseStoreFile
    {
        /// <summary>
        /// Name of file which stores database information.
        /// </summary>
        public const string NAME = "db.info";
    }
}