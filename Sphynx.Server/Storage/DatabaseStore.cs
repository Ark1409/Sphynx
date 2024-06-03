using System.Reflection;
using Sphynx.Utils;
using Sphynx.Storage;

namespace Sphynx.Server.Storage
{
    /// <summary>
    /// An <see cref="ISphynxStore{TLookup,TData}"/> backed by a database.
    /// </summary>
    /// <typeparam name="TKey">Identifier used to access a single data entity within the store, such as a primary key
    /// for a MySQL table row or a single document ID in MongoDB.</typeparam>
    /// <typeparam name="TContent">The type of data which this <see cref="DatabaseStore{TLookup,TValue}"/> stores at
    /// <see cref="TKey"/>. This would typically be a type describing a MySQL table row or MongoDB document.</typeparam>
    /// <remarks>A single <see cref="DatabaseStore{TLookup, TValue}"/> should represent a single storage system within
    /// a database, such as a MySQL table or a MongoDB collection.</remarks>
    public abstract class DatabaseStore<TKey, TContent> : IAsyncReadOnlySphynxStore<TKey, SphynxErrorInfo<TContent?>>,
        IAsyncSphynxStore<TKey, TContent> where TKey : notnull where TContent : IIdentifiable<TKey>
    {
        /// <inheritdoc/>
        public abstract Task<bool> PutAsync(TKey key, TContent data);

        public abstract Task<bool> InsertAsync(TContent data);

        public abstract Task<bool> UpdateAsync(TContent data);

        /// <summary>
        /// Upserts the value of a single field within a data entity held inside this store.
        /// </summary>
        /// <param name="key">The <see cref="TKey"/> in which the <paramref name="fieldName"/> is located.</param>
        /// <param name="fieldName">The name of the field to update.</param>
        /// <param name="value">The value to the field.</param>
        /// <typeparam name="TField">The data type of <paramref name="value"/>.</typeparam>
        /// <returns>true if the operation completed successfully; false otherwise.</returns>
        public abstract Task<bool> PutFieldAsync<TField>(TKey key, string fieldName, TField? value);

        /// <summary>
        /// Upserts the value of a single field within a data entity held inside this store.
        /// </summary>
        /// <param name="key">The <see cref="TKey"/> in which the <paramref name="field"/> is located.</param>
        /// <param name="field">The field to update.</param>
        /// <param name="value">The value to the field.</param>
        /// <typeparam name="TField">The data type of <paramref name="value"/>.</typeparam>
        /// <returns>true if the operation completed successfully; false otherwise.</returns>
        public virtual Task<bool> PutFieldAsync<TField>(TKey key, PropertyInfo field, TField? value) => PutFieldAsync(key, field.Name, value);

        /// <inheritdoc/>
        public virtual Task<SphynxErrorInfo<TContent?>> GetAsync(TKey key) => GetAsync(key, Array.Empty<string>());

        /// <inheritdoc cref="GetAsync(TKey)"/>
        /// <param name="excludedFields">The names of the fields to exclude from retrieval.</param>
        /// <returns>The data which is associated with the specified key, excluding the <paramref name="excludedFields"/>.</returns>
        public abstract Task<SphynxErrorInfo<TContent?>> GetAsync(TKey key, params string[] excludedFields);

        /// <summary>Gets the first item from this store which contains the provided <see cref="fieldName"/>
        /// with the specified <paramref name="value"/>.</summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">The name of the field to search.</param>
        /// <param name="value">The value of the field to match.</param>
        /// <returns>The first <see cref="TContent"/> with the specified field name and value found.</returns>
        public virtual Task<SphynxErrorInfo<TContent?>> GetWhereAsync<TField>(string fieldName, TField? value) =>
            GetWhereAsync(fieldName, value, Array.Empty<string>());

        /// <summary>Gets the first item from this store which contains the provided <see cref="fieldName"/>
        /// with the specified <paramref name="value"/>.</summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">The name of the field to search.</param>
        /// <param name="value">The value of the field to match.</param>
        /// <param name="excludedFields">The names of the fields to exclude from retrieval, when a <see cref="TContent"/> is found.</param>
        /// <returns>The first <see cref="TContent"/> with the specified field name and value found.</returns>
        public abstract Task<SphynxErrorInfo<TContent?>> GetWhereAsync<TField>(string fieldName, TField? value, params string[] excludedFields);

        /// <inheritdoc/>
        async Task<TContent?> IAsyncReadOnlySphynxStore<TKey, TContent>.GetAsync(TKey key) => (await GetAsync(key)).Data;

        /// <summary>
        /// Retrieves the value of a single field within a data entity held inside this store.
        /// </summary>
        /// <param name="key">The <see cref="TKey"/> in which the <paramref name="fieldName"/> is located.</param>
        /// <param name="fieldName">The name of the field value to retrieve.</param>
        /// <typeparam name="TField">The data type of the field.</typeparam>
        /// <returns>The value to the field.</returns>
        public abstract Task<SphynxErrorInfo<TField?>> GetFieldAsync<TField>(TKey key, string fieldName);

        /// <summary>
        /// Checks whether this store has <see cref="TContent"/> linked with the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The lookup for the <see cref="TContent"/>.</param>
        /// <returns>true if there is <see cref="TContent"/> which can be indexed with <paramref name="key"/>;
        /// false otherwise.</returns>
        public abstract Task<bool> ContainsAsync(TKey key);

        /// <summary>
        /// Checks whether this store has <see cref="TContent"/> which contains a field with the
        /// specified <see cref="fieldName"/>.
        /// </summary>
        /// <param name="fieldName">The name of the field to check.</param>
        /// <returns>true if there is <see cref="TContent"/> which contains a field with the specified
        /// name; false otherwise.</returns>
        public abstract Task<bool> ContainsFieldAsync(string fieldName);

        /// <inheritdoc/>
        public abstract Task<bool> DeleteAsync(TKey key);

        /// <summary>
        /// Deletes items from this store which contain a field with the specified value.
        /// </summary>
        /// <param name="fieldName">The name of the field.</param>
        /// <param name="value">The value of the field.</param>
        /// <typeparam name="TField">The type of the value of the field.</typeparam>
        /// <returns>The number of <see cref="TContent"/> deleted.</returns>
        public abstract Task<long> DeleteWhereAsync<TField>(string fieldName, TField value);

        #region Synchronous API

        /// <inheritdoc/>
        public bool Put(TKey index, TContent data) => PutAsync(index, data).GetAwaiter().GetResult();

        /// <inheritdoc cref="InsertAsync"/>
        public bool Insert(TContent data) => InsertAsync(data).GetAwaiter().GetResult();

        /// <inheritdoc cref="UpdateAsync"/>
        public bool Update(TContent data) => UpdateAsync(data).GetAwaiter().GetResult();

        /// <inheritdoc cref="PutFieldAsync{TValue}(TKey,PropertyInfo,TValue?)"/>
        public bool PutField<TValue>(TKey index, PropertyInfo field, TValue? value) => PutFieldAsync(index, field, value).GetAwaiter().GetResult();

        /// <inheritdoc cref="PutFieldAsync{TValue}(TKey,string,TValue?)"/>
        public bool PutField<TValue>(TKey index, string fieldName, TValue? value) => PutFieldAsync(index, fieldName, value).GetAwaiter().GetResult();

        /// <inheritdoc/>
        public SphynxErrorInfo<TContent?> Get(TKey index) => GetAsync(index).GetAwaiter().GetResult();

        /// <inheritdoc/>
        TContent? IReadOnlySphynxStore<TKey, TContent>.Get(TKey key) => Get(key).Data;

        /// <inheritdoc cref="GetFieldAsync{TValue}(TKey,string)"/>
        public SphynxErrorInfo<TValue?> GetField<TValue>(TKey key, string fieldName) =>
            GetFieldAsync<TValue>(key, fieldName).GetAwaiter().GetResult();

        /// <inheritdoc cref="ContainsFieldAsync"/>
        public bool ContainsField(string fieldName) => ContainsFieldAsync(fieldName).GetAwaiter().GetResult();

        /// <inheritdoc/>
        public bool Delete(TKey key) => DeleteAsync(key).GetAwaiter().GetResult();

        /// <inheritdoc cref="DeleteWhereAsync{TField}"/>
        public long DeleteWhere<TField>(string fieldName, TField value) => DeleteWhereAsync(fieldName, value).GetAwaiter().GetResult();

        #endregion
    }

    /// <summary>
    /// Holds name of file which stores database information.
    /// </summary>
    public static class DatabaseInfoFile
    {
        /// <summary>
        /// Name of file which stores database information.
        /// </summary>
        public const string NAME = "db.info";
    }
}