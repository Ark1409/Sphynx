namespace Sphynx.Core
{
    /// <summary>
    /// A result type holding solely error information.
    /// </summary>
    /// <param name="ErrorCode">The error code for the operation.</param>
    /// <param name="Message">A descriptive message for the error.</param>
    public readonly record struct SphynxErrorInfo(SphynxErrorCode ErrorCode, string? Message = null)
    {
        public SphynxErrorInfo<TData> WithData<TData>(TData data) => new SphynxErrorInfo<TData>(ErrorCode, Message, data);
        public SphynxErrorInfo<TData?> WithData<TData>(TData? data) where TData : struct => new SphynxErrorInfo<TData?>(ErrorCode, Message, data);

        /// <summary>
        /// Returns a new <see cref="SphynxErrorInfo"/> with the specified error code.
        /// </summary>
        /// <param name="error">The error code for this <see cref="SphynxErrorInfo"/>.</param>
        /// <returns>The data for this <see cref="SphynxErrorInfo"/>.</returns>
        public static implicit operator SphynxErrorInfo(SphynxErrorCode error) => new(error);
    };

    /// <summary>
    /// A result type holding error information and the underlying data.
    /// </summary>
    /// <param name="ErrorCode">The error code for the operation.</param>
    /// <param name="Message">A descriptive message for the error.</param>
    /// <param name="Data">The return data.</param>
    /// <typeparam name="TData">The type data held by this result on success.</typeparam>
    public readonly record struct SphynxErrorInfo<TData>(SphynxErrorCode ErrorCode, string? Message = null, TData? Data = default)
        : IEquatable<TData?>
    {
        /// <summary>
        /// Creates a new <see cref="SphynxErrorInfo{TData}"/> with <see cref="SphynxErrorCode.SUCCESS"/>.
        /// </summary>
        /// <param name="data">The data to store.</param>
        public SphynxErrorInfo(TData data) : this(SphynxErrorCode.SUCCESS, null, data)
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
        public static implicit operator TData?(SphynxErrorInfo<TData?> other) => other.Data;

        /// <summary>
        /// Returns a <see cref="SphynxErrorInfo{TData}"/> for the <paramref cref="data"/>.
        /// </summary>
        /// <param name="data">The data to encapsulate.</param>
        /// <returns>A new <see cref="SphynxErrorInfo{TData}"/> with the underlying <paramref name="data"/>.</returns>
        public static implicit operator SphynxErrorInfo<TData>(TData data) => new(data);

        /// <summary>
        /// Returns a new <see cref="SphynxErrorInfo{TData}"/> with <see langword="default"/> <see cref="Data"/>.
        /// </summary>
        /// <param name="error">The error code for this <see cref="SphynxErrorInfo{TData}"/>.</param>
        /// <returns>The data for this <see cref="SphynxErrorInfo{TData}"/>.</returns>
        public static implicit operator SphynxErrorInfo<TData>(SphynxErrorCode error) => new(error);

        /// <summary>
        /// Returns a new <see cref="SphynxErrorInfo"/> with the same <see cref="ErrorCode"/> and <see cref="Message"/>
        /// as this <see cref="SphynxErrorInfo{T}"/>.
        /// </summary>
        /// <param name="error">The error code for this <see cref="SphynxErrorInfo"/>.</param>
        /// <returns>The data for this <see cref="SphynxErrorInfo"/>.</returns>
        public static implicit operator SphynxErrorInfo(SphynxErrorInfo<TData> error) => new(error.ErrorCode, error.Message);

        /// <summary>
        /// Returns a new <see cref="SphynxErrorInfo{TData}"/> with <see langword="default"/> <see cref="Data"/>.
        /// </summary>
        /// <param name="error">The error code for this <see cref="SphynxErrorInfo{TData}"/>.</param>
        /// <returns>The data for this <see cref="SphynxErrorInfo{TData}"/>.</returns>
        public static explicit operator SphynxErrorInfo<TData>(SphynxErrorInfo error) => new(error.ErrorCode, error.Message);

        /// <inheritdoc/>
        public bool Equals(TData? other)
        {
            if (Data is null && other is null) return true;
            if (Data is null || other is null) return false;

            return Data!.Equals(other);
        }
    }
}
