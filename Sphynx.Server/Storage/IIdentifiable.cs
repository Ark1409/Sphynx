namespace Sphynx.Server.Storage
{
    /// <summary>
    /// Represents an entity that is identifiable by a <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The identifier type.</typeparam>
    public interface IIdentifiable<out T>
    {
        /// <summary>
        /// The <see cref="T"/> which identifies an entity.
        /// </summary>
        public T Id { get; }
    }
}