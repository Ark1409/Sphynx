using Sphynx.Packet;

namespace Sphynx.Server.Utils
{
    /// <summary>
    /// A type representing a wrapping for a TryXXX-style method meant to be used within asynchronous code.
    /// </summary>
    /// <param name="ErrorCode">The error code for the operation.</param>
    /// <param name="Data">The return data.</param>
    /// <typeparam name="TData">The data that this info type holds.</typeparam>
    public record struct SphynxErrorInfo<TData>(SphynxErrorCode ErrorCode, TData? Data) : IEquatable<TData?>
    {
        public bool Equals(TData? other)
        {
            if (Data is null && other is null) return true;
            if (Data is null || other is null) return false;

            return Data!.Equals(other);
        }
    }
}