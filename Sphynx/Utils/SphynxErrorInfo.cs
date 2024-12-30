using Sphynx.Network.Packet;

namespace Sphynx.Utils
{
    /// <summary>
    /// A type representing a wrapping for a TryXXX-style method purpsoed for asynchronous code.
    /// </summary>
    /// <param name="ErrorCode">The error code for the operation.</param>
    /// <param name="Data">The return data.</param>
    /// <typeparam name="TData">The data held by this info type.</typeparam>
    public record struct SphynxErrorInfo<TData>(SphynxErrorCode ErrorCode, TData? Data = default) : IEquatable<TData?>
    {
        /// <summary>
        /// Creates a new <see cref="SphynxErrorInfo{TData}"/> with <see cref="SphynxErrorCode.SUCCESS"/>.
        /// </summary>
        /// <param name="data">The data to store.</param>
        public SphynxErrorInfo(TData? data) : this(SphynxErrorCode.SUCCESS, data)
        {
        }
        
        /// <summary>
        /// Returns whether <see cref="ErrorCode"/> is <see cref="SphynxErrorCode.SUCCESS"/>.
        /// </summary>
        /// <param name="other">The object from which to retrieve the <see cref="ErrorCode"/>.</param>
        /// <returns>Whether <see cref="ErrorCode"/> is <see cref="SphynxErrorCode.SUCCESS"/>.</returns>
        public static implicit operator bool(SphynxErrorInfo<TData> other) => other.ErrorCode == SphynxErrorCode.SUCCESS;

        /// <summary>
        /// Returns the <see cref="Data"/> for this <see cref="SphynxErrorInfo{TData}"/>.
        /// </summary>
        /// <param name="other">The object from which to retrieve the <see cref="Data"/>.</param>
        /// <returns>The data for this <see cref="SphynxErrorInfo{TData}"/>.</returns>
        public static implicit operator TData?(SphynxErrorInfo<TData> other) => other.Data;
        
        /// <summary>
        /// Returns a new <see cref="SphynxErrorInfo{TData}"/> with <see langword="default"/> <see cref="Data"/>.
        /// </summary>
        /// <param name="error">The error code for this <see cref="SphynxErrorInfo{TData}"/>.</param>
        /// <returns>The data for this <see cref="SphynxErrorInfo{TData}"/>.</returns>
        public static implicit operator SphynxErrorInfo<TData>?(SphynxErrorCode error) => new SphynxErrorInfo<TData>(error);
        
        /// <inheritdoc/>
        public bool Equals(TData? other)
        {
            if (Data is null && other is null) return true;
            if (Data is null || other is null) return false;

            return Data!.Equals(other);
        }
    }
}