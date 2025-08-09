// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Sphynx.Core
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
        public static readonly SnowflakeId Empty = default;

        public static readonly SnowflakeId MaxValue = new(DateTimeOffset.MaxValue.ToUnixTimeMilliseconds(), -1);
        public static readonly SnowflakeId MinValue = new(DateTimeOffset.MinValue.ToUnixTimeMilliseconds(), 0);

        private readonly long _a; // timestamp
        private readonly int _b; // sequence number + machine id

        /// <summary>
        /// Returns the timestamp for this id.
        /// </summary>
        public long Timestamp => _a;

        public DateTimeOffset DateTime => DateTimeOffset.FromUnixTimeMilliseconds(Timestamp);

        /// <summary>
        /// Returns the sequence number for this id.
        /// </summary>
        public int SequenceNumber => _b >> 16;

        /// <summary>
        /// Returns the machine info associated with this id.
        /// </summary>
        public int MachineId => _b & 0xffff;

        /// <summary>
        /// Creates a new <see cref="SnowflakeId"/> from the provided arguments.
        /// </summary>
        /// <param name="timestamp">The timestamp for this id.</param>
        /// <param name="sequenceNumber">The sequence number for this id.</param>
        /// <param name="machineId">The machine info for this id.</param>
        [CLSCompliant(false)]
        public SnowflakeId(long timestamp, ushort sequenceNumber, ushort machineId)
        {
            _a = timestamp;
            _b = (sequenceNumber << 16) | machineId;
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

            _a = BinaryPrimitives.ReadInt64LittleEndian(bytes);
            _b = BinaryPrimitives.ReadInt32LittleEndian(bytes[sizeof(long)..]);
        }

        /// <summary>
        /// Creates a new <see cref="SnowflakeId"/> from a string value.
        /// </summary>
        /// <param name="value">The string content of the <see cref="SnowflakeId"/>, as generated from
        /// <see cref="ToString"/>.</param>
        public SnowflakeId(string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException($"String '{value}' is not of correct length.", nameof(value));

            if (!TryParse(value.AsSpan(), out var result))
                throw new ArgumentException($"'{value}' is not a valid {nameof(SnowflakeId)}.");

            this = result.Value;
        }

        private SnowflakeId(long a, int b)
        {
            _a = a;
            _b = b;
        }

        /// <summary>
        /// Attempts to create a new <see cref="SnowflakeId"/> from the input string value.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="snowflakeId">The resulting <see cref="SnowflakeId"/>.</param>
        /// <returns>true if the parse operation was successful; false otherwise.</returns>
        public static bool TryParse(ReadOnlySpan<char> input, [NotNullWhen(true)] out SnowflakeId? snowflakeId)
        {
            // Each byte requires two chars in hex
            const int CHARS_PER_BYTE = 2;
            const int LENGTH = SIZE * CHARS_PER_BYTE;

            if (input.Length != LENGTH)
            {
                snowflakeId = null;
                return false;
            }

            if (!TryParseTimestamp(input, out long timestamp) ||
                !TryParseSequenceAndMachine(input[(CHARS_PER_BYTE * sizeof(long))..], out int sequenceMachine))
            {
                snowflakeId = null;
                return false;
            }

            snowflakeId = new SnowflakeId(timestamp, sequenceMachine);
            return true;
        }

        private static bool TryParseSequenceAndMachine(ReadOnlySpan<char> input, out int value)
        {
            // Each byte requires two chars in hex
            const int CHARS_PER_BYTE = 2;
            const int SEQUENCE_MACHINE_LENGTH = sizeof(int) * CHARS_PER_BYTE;

            Debug.Assert(input.Length >= SEQUENCE_MACHINE_LENGTH);

            var sequenceAndMachineBytes = input[..SEQUENCE_MACHINE_LENGTH];

            return int.TryParse(sequenceAndMachineBytes, NumberStyles.AllowHexSpecifier, null, out value);
        }

        private static bool TryParseTimestamp(ReadOnlySpan<char> input, out long value)
        {
            // Each byte requires two chars in hex
            const int CHARS_PER_BYTE = 2;
            const int TIMESTAMP_LENGTH = sizeof(long) * CHARS_PER_BYTE;

            Debug.Assert(input.Length >= TIMESTAMP_LENGTH);

            var timestampBytes = input[..TIMESTAMP_LENGTH];

            return long.TryParse(timestampBytes, NumberStyles.AllowHexSpecifier, null, out value);
        }

        /// <summary>
        /// Serializes this <see cref="SnowflakeId"/> into a byte array.
        /// </summary>
        /// <returns>This id as a byte array.</returns>
        public byte[] ToByteArray()
        {
            byte[] bytes = new byte[SIZE];

            bool written = TryWriteBytes(bytes);
            Debug.Assert(written);

            return bytes;
        }

        /// <summary>
        /// Serializes this <see cref="SnowflakeId"/> into the span.
        /// </summary>
        /// <param name="destination">The destination span.</param>
        /// <returns>true if <paramref name="destination"/> is of length 12 or greater; false otherwise.</returns>
        public bool TryWriteBytes(Span<byte> destination)
        {
            if (destination.Length < SIZE)
            {
                return false;
            }

            BinaryPrimitives.WriteInt64LittleEndian(destination, _a);
            BinaryPrimitives.WriteInt32LittleEndian(destination[sizeof(long)..], _b);
            return true;
        }

        /// <inheritdoc cref="Equals(SnowflakeId)"/>
        public bool Equals(in SnowflakeId other)
        {
            return _a == other._a && _b == other._b;
        }

        /// <inheritdoc />
        public bool Equals(SnowflakeId other)
        {
            return Equals(in other);
        }

        /// <inheritdoc cref="Equals(in Sphynx.Core.SnowflakeId)"/>
        public bool Equals(SnowflakeId? other)
        {
            return other.HasValue && Equals(other.Value);
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
            // TODO: Trim to 20 chars (DateTimeOffset.MaxValue)
            const int CHARS_PER_BYTE = 2;

            string hex = string.Create(SIZE * CHARS_PER_BYTE, this, static (span, inst) =>
            {
                bool formatted = inst._a.TryFormat(span, out _, format: "x16");
                formatted &= inst._b.TryFormat(span[(CHARS_PER_BYTE * sizeof(long))..], out _, format: "x8");

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
            return lhs.Equals(in rhs);
        }

        /// <summary>
        /// Compares two <see cref="SnowflakeId"/>s.
        /// </summary>
        /// <param name="lhs">The first <see cref="SnowflakeId"/>.</param>
        /// <param name="rhs">The other <see cref="SnowflakeId"/>.</param>
        /// <returns>true if <paramref name="lhs"/> is not equal to <paramref name="rhs"/>;
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

        /// <inheritdoc cref="CompareTo(SnowflakeId)"/>
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
        /// <remarks>
        /// For timestamps within the same millisecond and with the same sequence number,
        /// the machine id bits are the only ones left to compare, but may not provide a total order.
        /// </remarks>
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
