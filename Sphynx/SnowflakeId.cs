// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Sphynx
{
    /// <summary>
    /// Represents a 96-bit unique identifier which can be ordered by time.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly partial struct SnowflakeId : IEquatable<SnowflakeId>, IComparable, IComparable<SnowflakeId>
    {
        /// <summary>
        /// The size of a single <see cref="SnowflakeId"/> in bytes.
        /// </summary>
        public const int SIZE = 12;

        /// <summary>
        /// Represents an empty instance which should never be obtainable through generation.
        /// </summary>
        public static SnowflakeId Empty = default;

        private readonly long _a; // timestamp
        private readonly int _b; // machine id + sequence number

        /// <summary>
        /// Returns the timestamp for this id.
        /// </summary>
        public long Timestamp => _a;

        /// <summary>
        /// Returns the machine info associated with this id.
        /// </summary>
        public int MachineId => (int)(_b >> 16);

        /// <summary>
        /// Returns the sequence number for this id.
        /// </summary>
        public int SequenceNumber => (int)(_b & 0xffff);

        /// <summary>
        /// Creates a new <see cref="SnowflakeId"/> from the provided arguments.
        /// </summary>
        /// <param name="timestamp">The timestamp for this id.</param>
        /// <param name="machineId">The machine info for this id.</param>
        /// <param name="sequenceNumber">The sequence number for this id.</param>
        [CLSCompliant(false)]
        public SnowflakeId(long timestamp, ushort machineId, ushort sequenceNumber)
        {
            _a = timestamp;
            _b = (machineId << 16) | sequenceNumber;
        }

        /// <inheritdoc />
        public SnowflakeId(byte[] bytes)
            : this(new ReadOnlySpan<byte>(bytes ?? throw new ArgumentNullException(nameof(bytes))))
        {
        }

        /// <summary>
        /// Creates a new <see cref="SnowflakeId"/> from a span of 12 bytes.
        /// </summary>
        /// <param name="bytes">A span of 12 bytes from which to read the <see cref="SnowflakeId"/> content.</param>
        /// <exception cref="ArgumentException">If <paramref name="bytes"/> is not 12 bytes.</exception>
        public SnowflakeId(ReadOnlySpan<byte> bytes)
        {
            if ((uint)bytes.Length != SIZE)
            {
                throw new ArgumentException($"Length of {nameof(bytes)} must be equal to {SIZE}");
            }

            if (BitConverter.IsLittleEndian)
            {
                this = MemoryMarshal.Read<SnowflakeId>(bytes);
                return;
            }

            _a = BinaryPrimitives.ReadInt64LittleEndian(bytes);
            _b = BinaryPrimitives.ReadInt32LittleEndian(bytes[sizeof(long)..]);
        }

        /// <summary>
        /// Serializes this <see cref="SnowflakeId"/> into a byte array.
        /// </summary>
        /// <returns>This id as a byte array.</returns>
        public byte[] ToByteArray()
        {
            byte[] bytes = new byte[SIZE];
            Debug.Assert(TryWriteBytes(bytes));
            return bytes;
        }

        /// <summary>
        /// Serializes this <see cref="SnowflakeId"/> into the span.
        /// </summary>
        /// <param name="destination">The destination span.</param>
        /// <returns>true if <paramref name="destination"/> is of length 12; false otherwise.</returns>
        public bool TryWriteBytes(Span<byte> destination)
        {
            if (BitConverter.IsLittleEndian)
            {
                return MemoryMarshal.TryWrite(destination, ref Unsafe.AsRef(in this));
            }

            if (destination.Length < SIZE)
            {
                return false;
            }

            BinaryPrimitives.WriteInt64LittleEndian(destination, _a);
            BinaryPrimitives.WriteInt32LittleEndian(destination[sizeof(long)..], _b);
            return true;
        }

        /// <inheritdoc cref="Equals(Sphynx.SnowflakeId)"/>
        public bool Equals(in SnowflakeId other)
        {
            return _a == other._a && _b == other._b;
        }

        /// <inheritdoc />
        public bool Equals(SnowflakeId other)
        {
            return Equals(in other);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is SnowflakeId other && Equals(in other);
        }

        /// <summary>
        /// Returns a hexadecimal representation of this <see cref="SnowflakeId"/>.
        /// </summary>
        /// <returns>A hexadecimal representation of this <see cref="SnowflakeId"/>.</returns>
        public override string ToString()
        {
            // Each byte requires two chars in hex
            const int CHARS_PER_BYTE = 2;
            const int LENGTH = SIZE * CHARS_PER_BYTE;

            string hex = string.Create(LENGTH, this, (span, inst) =>
            {
                bool formatted = inst._a.TryFormat(span, out _, format: "x16");
                formatted &= inst._b.TryFormat(span[(sizeof(long) * CHARS_PER_BYTE)..], out _, format: "x8");

                Debug.Assert(formatted);
            });

            return hex;
        }

        /// <summary>
        /// Returns the hash code for this <see cref="SnowflakeId"/>.
        /// </summary>
        /// <returns>The hash code for this <see cref="SnowflakeId"/>.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(_a, _b);
        }

        /// <summary>
        /// Compares two <see cref="SnowflakeId"/>s.
        /// </summary>
        /// <param name="lhs">The first <see cref="SnowflakeId"/>.</param>
        /// <param name="rhs">The other <see cref="SnowflakeId"/></param>
        /// <returns>true if <paramref name="lhs"/> is less than to <paramref name="rhs"/>;
        /// false otherwise.</returns>
        public static bool operator <(SnowflakeId lhs, SnowflakeId rhs)
        {
            return lhs.CompareTo(rhs) < 0;
        }

        /// <summary>
        /// Compares two <see cref="SnowflakeId"/>s.
        /// </summary>
        /// <param name="lhs">The first <see cref="SnowflakeId"/>.</param>
        /// <param name="rhs">The other <see cref="SnowflakeId"/></param>
        /// <returns>true if <paramref name="lhs"/> is less than or equal to <paramref name="rhs"/>;
        /// false otherwise.</returns>
        public static bool operator <=(SnowflakeId lhs, SnowflakeId rhs)
        {
            return lhs.CompareTo(rhs) <= 0;
        }

        /// <summary>
        /// Compares two <see cref="SnowflakeId"/>s.
        /// </summary>
        /// <param name="lhs">The first <see cref="SnowflakeId"/>.</param>
        /// <param name="rhs">The other <see cref="SnowflakeId"/>.</param>
        /// <returns>true if <paramref name="lhs"/> are equal to <paramref name="rhs"/>;
        /// false otherwise.</returns>
        public static bool operator ==(SnowflakeId lhs, SnowflakeId rhs)
        {
            return lhs.Equals(rhs);
        }

        /// <summary>
        /// Compares two <see cref="SnowflakeId"/>s.
        /// </summary>
        /// <param name="lhs">The first <see cref="SnowflakeId"/>.</param>
        /// <param name="rhs">The other <see cref="SnowflakeId"/>.</param>
        /// <returns>true if <paramref name="lhs"/> is equal to <paramref name="rhs"/>;
        /// false otherwise.</returns>
        public static bool operator !=(SnowflakeId lhs, SnowflakeId rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>
        /// Compares two <see cref="SnowflakeId"/>s.
        /// </summary>
        /// <param name="lhs">The first <see cref="SnowflakeId"/>.</param>
        /// <param name="rhs">The other <see cref="SnowflakeId"/></param>
        /// <returns>true if <paramref name="lhs"/> is greater than or equal to
        /// <paramref name="rhs"/>; false otherwise.</returns>
        public static bool operator >=(SnowflakeId lhs, SnowflakeId rhs)
        {
            return lhs.CompareTo(rhs) >= 0;
        }

        /// <summary>
        /// Compares two <see cref="SnowflakeId"/>s.
        /// </summary>
        /// <param name="lhs">The first <see cref="SnowflakeId"/>.</param>
        /// <param name="rhs">The other <see cref="SnowflakeId"/></param>
        /// <returns>true if <paramref name="lhs"/> is greater than <paramref name="rhs"/>; false otherwise.</returns>
        public static bool operator >(SnowflakeId lhs, SnowflakeId rhs)
        {
            return lhs.CompareTo(rhs) > 0;
        }

        /// <inheritdoc cref="CompareTo(Sphynx.SnowflakeId)"/>
        public int CompareTo(in SnowflakeId other)
        {
            if (_a != other._a)
            {
                return _a < other._a ? -1 : 1;
            }

            if (_b != other._b)
            {
                return (uint)_b < (uint)other._b ? -1 : 1;
            }

            return 0;
        }

        /// <inheritdoc />
        /// <remarks>For timestamps within the same millisecond and with the same sequence number,
        /// the machine id will be the only bits left to compare, but may not provide a total order.</remarks>
        public int CompareTo(SnowflakeId other)
        {
            return CompareTo(in other);
        }

        /// <inheritdoc />
        public int CompareTo(object? obj)
        {
            if (obj is null)
            {
                return 1;
            }

            if (obj is SnowflakeId other)
            {
                return CompareTo(in other);
            }

            throw new ArgumentException($"Object must be of type {nameof(SnowflakeId)}");
        }
    }
}
