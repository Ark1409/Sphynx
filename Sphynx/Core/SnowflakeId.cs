// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace Sphynx.Core
{
    /// <summary>
    /// Represents an 80-bit unique identifier which can be ordered by time.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    [DebuggerDisplay("{ToString(),nq}")]
    [JsonConverter(typeof(SnowflakeIdConverter))]
    public readonly partial struct SnowflakeId : IEquatable<SnowflakeId>, IComparable, IComparable<SnowflakeId>
    {
        /// <summary>
        /// The size of a single <see cref="SnowflakeId"/> in bytes.
        /// </summary>
        public const int SIZE = 10;

        /// <summary>
        /// Represents an empty instance which should never be obtainable through generation.
        /// </summary>
        public static readonly SnowflakeId Empty = default;

        public static readonly SnowflakeId MaxValue = new(DateTimeOffset.MaxValue.ToUnixTimeMilliseconds(), ushort.MaxValue, ushort.MaxValue);
        public static readonly SnowflakeId MinValue = new(DateTimeOffset.MinValue.ToUnixTimeMilliseconds(), (ushort)0, (ushort)0);

        [FieldOffset(0)] private readonly byte _timestamp0; // timestamp HOB
        [FieldOffset(1)] private readonly byte _timestamp1; // timestamp
        [FieldOffset(2)] private readonly byte _timestamp2; // timestamp
        [FieldOffset(3)] private readonly byte _timestamp3; // timestamp
        [FieldOffset(4)] private readonly byte _timestamp4; // timestamp
        [FieldOffset(5)] private readonly byte _timestamp5; // timestamp LOB

        [FieldOffset(6)] private readonly byte _sm0; // sequence number + machine id HOB
        [FieldOffset(7)] private readonly byte _sm1; // sequence number + machine id
        [FieldOffset(8)] private readonly byte _sm2; // sequence number + machine id
        [FieldOffset(9)] private readonly byte _sm3; // sequence number + machine id LOB

        /// <summary>
        /// Returns the timestamp for this id.
        /// </summary>
        [IgnoreDataMember]
        [JsonIgnore]
        [SoapIgnore]
        public long Timestamp
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                uint abcd = Unsafe.ReadUnaligned<uint>(ref Unsafe.AsRef(_timestamp0));
                ushort ef = Unsafe.ReadUnaligned<ushort>(ref Unsafe.AsRef(_timestamp4));

                if (BitConverter.IsLittleEndian)
                {
                    // Address increases to the right
                    // A|B|C|D|E|F  ->  F|E|D|C|B|A|0|0
                    //              ->  F|E|0|0|0|0|0|0
                    //                + 0|0|D|C|B|A|0|0
                    return (long)BinaryPrimitives.ReverseEndianness(ef) + ((long)BinaryPrimitives.ReverseEndianness(abcd) << 16);
                }
                else
                {
                    // Address increases to the right
                    // A|B|C|D|E|F  ->  0|0|A|B|C|D|E|F
                    //              ->  0|0|A|B|C|D|0|0
                    //                +             E|F
                    return ((long)abcd << 16) + ef;
                }
            }
        }

        [IgnoreDataMember]
        [JsonIgnore]
        [SoapIgnore]
        public DateTimeOffset DateTime
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => DateTimeOffset.FromUnixTimeMilliseconds(Timestamp);
        }

        /// <summary>
        /// Returns the sequence number for this id.
        /// </summary>
        [IgnoreDataMember]
        [JsonIgnore]
        [SoapIgnore]
        public int SequenceNumber
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ushort s = Unsafe.ReadUnaligned<ushort>(ref Unsafe.AsRef(_sm0));
                return BitConverter.IsLittleEndian ? (int)BinaryPrimitives.ReverseEndianness(s) : (int)s;
            }
        }

        /// <summary>
        /// Returns the machine info associated with this id.
        /// </summary>
        [IgnoreDataMember]
        [JsonIgnore]
        [SoapIgnore]
        public int MachineId
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ushort m = Unsafe.ReadUnaligned<ushort>(ref Unsafe.AsRef(_sm2));
                return BitConverter.IsLittleEndian ? (int)BinaryPrimitives.ReverseEndianness(m) : (int)m;
            }
        }

        /// <summary>
        /// Creates a new <see cref="SnowflakeId"/> from the provided arguments.
        /// </summary>
        /// <param name="timestamp">The timestamp for this id.</param>
        /// <param name="sequenceNumber">The sequence number for this id.</param>
        /// <param name="machineId">The machine info for this id.</param>
        public SnowflakeId(long timestamp, short sequenceNumber, short machineId)
            : this(timestamp < MinValue.Timestamp || timestamp > MaxValue.Timestamp
                    ? throw new ArgumentOutOfRangeException(nameof(timestamp), timestamp,
                        $"Timestamp must in range [{MinValue.Timestamp}, {MaxValue.Timestamp}]")
                    : timestamp,
                Unsafe.As<short, ushort>(ref Unsafe.AsRef(sequenceNumber)),
                Unsafe.As<short, ushort>(ref Unsafe.AsRef(machineId)))
        {
        }

        private SnowflakeId(long timestamp, ushort sequenceNumber, ushort machineId)
        {
            unsafe
            {
                ref byte timestampBytes = ref Unsafe.AsRef<byte>(Unsafe.AsPointer(ref timestamp));
                ref byte sequenceBytes = ref Unsafe.AsRef<byte>(Unsafe.AsPointer(ref sequenceNumber));
                ref byte machineBytes = ref Unsafe.AsRef<byte>(Unsafe.AsPointer(ref machineId));

                if (BitConverter.IsLittleEndian)
                {
                    // Only take the lower 48 bits (MaxValue)
                    // Store everything in "big-endian"
                    _timestamp0 = Unsafe.Add(ref timestampBytes, 5);
                    _timestamp1 = Unsafe.Add(ref timestampBytes, 4);
                    _timestamp2 = Unsafe.Add(ref timestampBytes, 3);
                    _timestamp3 = Unsafe.Add(ref timestampBytes, 2);
                    _timestamp4 = Unsafe.Add(ref timestampBytes, 1);
                    _timestamp5 = Unsafe.Add(ref timestampBytes, 0);

                    _sm0 = Unsafe.Add(ref sequenceBytes, 1);
                    _sm1 = sequenceBytes;
                    _sm2 = Unsafe.Add(ref machineBytes, 1);
                    _sm3 = machineBytes;
                }
                else
                {
                    // Only take the lower 48 bits (MaxValue)
                    // Store everything in "big-endian"
                    _timestamp0 = Unsafe.Add(ref timestampBytes, 2);
                    _timestamp1 = Unsafe.Add(ref timestampBytes, 3);
                    _timestamp2 = Unsafe.Add(ref timestampBytes, 4);
                    _timestamp3 = Unsafe.Add(ref timestampBytes, 5);
                    _timestamp4 = Unsafe.Add(ref timestampBytes, 6);
                    _timestamp5 = Unsafe.Add(ref timestampBytes, 7);

                    _sm0 = sequenceBytes;
                    _sm1 = Unsafe.Add(ref sequenceBytes, 1);
                    _sm2 = machineBytes;
                    _sm3 = Unsafe.Add(ref machineBytes, 1);
                }
            }
        }

        /// <inheritdoc />
        public SnowflakeId(byte[] bytes)
            : this(new ReadOnlySpan<byte>(bytes ?? throw new ArgumentNullException(nameof(bytes))))
        {
        }

        /// <summary>
        /// Creates a new <see cref="SnowflakeId"/> from a span of <see cref="SIZE"/> bytes.
        /// </summary>
        /// <param name="bytes">A span of <see cref="SIZE"/> bytes from which to read the <see cref="SnowflakeId"/> content.</param>
        /// <exception cref="ArgumentException">If <paramref name="bytes"/> is not <see cref="SIZE"/> bytes.</exception>
        public SnowflakeId(ReadOnlySpan<byte> bytes) : this()
        {
            if (bytes.Length != SIZE)
                throw new ArgumentException($"Length of {nameof(bytes)} must be equal to {SIZE}", nameof(bytes));

            bytes.CopyTo(MemoryMarshal.CreateSpan(ref Unsafe.AsRef(_timestamp0), SIZE));
        }

        /// <summary>
        /// Creates a new <see cref="SnowflakeId"/> from a string value.
        /// </summary>
        /// <param name="value">The string content of the <see cref="SnowflakeId"/>.</param>
        public SnowflakeId(string value)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (!TryParse(value.AsSpan(), out var result))
                throw new ArgumentException($"'{value}' is not a valid {nameof(SnowflakeId)}.", nameof(value));

            this = result.Value;
        }

        /// <summary>
        /// Creates a new <see cref="SnowflakeId"/> from a string value.
        /// </summary>
        /// <param name="value">The string content of the <see cref="SnowflakeId"/>.</param>
        public SnowflakeId(ReadOnlySpan<char> value)
        {
            if (!TryParse(value, out var result))
                throw new ArgumentException($"'{value}' is not a valid {nameof(SnowflakeId)}.", nameof(value));

            this = result.Value;
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
        /// <returns>true if <paramref name="destination"/> is of length <see cref="SIZE"/> or greater; false otherwise.</returns>
        public bool TryWriteBytes(Span<byte> destination)
        {
            if (destination.Length < SIZE)
            {
                return false;
            }

            ulong abcdefgh = Unsafe.ReadUnaligned<ulong>(ref Unsafe.AsRef(_timestamp0));
            ushort ij = Unsafe.ReadUnaligned<ushort>(ref Unsafe.AsRef(_sm2));

            ref byte destRef = ref MemoryMarshal.GetReference(destination);
            Unsafe.WriteUnaligned(ref destRef, abcdefgh);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destRef, sizeof(ulong)), ij);

            return true;
        }

        /// <inheritdoc cref="Equals(SnowflakeId)"/>
        public bool Equals(in SnowflakeId other)
        {
            ref byte thisRef = ref Unsafe.AsRef(_timestamp0);
            ref byte otherRef = ref Unsafe.AsRef(other._timestamp0);

            // Endianness doesn't matter for equality

            ulong upper = Unsafe.ReadUnaligned<ulong>(ref thisRef);
            ulong otherUpper = Unsafe.ReadUnaligned<ulong>(ref otherRef);

            if (upper != otherUpper)
                return false;

            ushort lower = Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref thisRef, sizeof(ulong)));
            ushort otherLower = Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref otherRef, sizeof(ulong)));

            return lower == otherLower;
        }

        /// <inheritdoc />
        public bool Equals(SnowflakeId other)
        {
            return Equals(in other);
        }

        /// <inheritdoc cref="Equals(in Sphynx.Core.SnowflakeId)"/>
        public bool Equals([NotNullWhen(true)] SnowflakeId? other)
        {
            return other.HasValue && Equals(other.Value);
        }

        /// <inheritdoc />
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is SnowflakeId other && Equals(in other);
        }

        /// <summary>
        /// Returns the hash code for this <see cref="SnowflakeId"/>.
        /// </summary>
        /// <returns>The hash code for this <see cref="SnowflakeId"/>.</returns>
        public override int GetHashCode()
        {
            var hashCode = new HashCode();

            var thisSpan = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(_timestamp0), SIZE);
            hashCode.AddBytes(thisSpan);

            return hashCode.ToHashCode();
        }

        /// <summary>
        /// Compares two <see cref="SnowflakeId"/>s.
        /// </summary>
        /// <param name="lhs">The first <see cref="SnowflakeId"/>.</param>
        /// <param name="rhs">The other <see cref="SnowflakeId"/></param>
        /// <returns>true if <paramref name="lhs"/> is less than to <paramref name="rhs"/>;
        /// false otherwise.</returns>
        public static bool operator <(in SnowflakeId lhs, in SnowflakeId rhs)
        {
            return lhs.CompareTo(in rhs) < 0;
        }

        /// <summary>
        /// Compares two <see cref="SnowflakeId"/>s.
        /// </summary>
        /// <param name="lhs">The first <see cref="SnowflakeId"/>.</param>
        /// <param name="rhs">The other <see cref="SnowflakeId"/></param>
        /// <returns>true if <paramref name="lhs"/> is less than or equal to <paramref name="rhs"/>;
        /// false otherwise.</returns>
        public static bool operator <=(in SnowflakeId lhs, in SnowflakeId rhs)
        {
            return lhs.CompareTo(in rhs) <= 0;
        }

        /// <summary>
        /// Compares two <see cref="SnowflakeId"/>s.
        /// </summary>
        /// <param name="lhs">The first <see cref="SnowflakeId"/>.</param>
        /// <param name="rhs">The other <see cref="SnowflakeId"/>.</param>
        /// <returns>true if <paramref name="lhs"/> are equal to <paramref name="rhs"/>;
        /// false otherwise.</returns>
        public static bool operator ==(in SnowflakeId lhs, in SnowflakeId rhs)
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
        public static bool operator !=(in SnowflakeId lhs, in SnowflakeId rhs)
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
        public static bool operator >=(in SnowflakeId lhs, in SnowflakeId rhs)
        {
            return lhs.CompareTo(in rhs) >= 0;
        }

        /// <summary>
        /// Compares two <see cref="SnowflakeId"/>s.
        /// </summary>
        /// <param name="lhs">The first <see cref="SnowflakeId"/>.</param>
        /// <param name="rhs">The other <see cref="SnowflakeId"/></param>
        /// <returns>true if <paramref name="lhs"/> is greater than <paramref name="rhs"/>; false otherwise.</returns>
        public static bool operator >(in SnowflakeId lhs, in SnowflakeId rhs)
        {
            return lhs.CompareTo(in rhs) > 0;
        }

        /// <inheritdoc cref="CompareTo(SnowflakeId)"/>
        public int CompareTo(in SnowflakeId other)
        {
            ref byte thisRef = ref Unsafe.AsRef(_timestamp0);
            ref byte otherRef = ref Unsafe.AsRef(other._timestamp0);

            if (BitConverter.IsLittleEndian)
            {
                ulong upper = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ulong>(ref thisRef));
                ulong otherUpper = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ulong>(ref otherRef));

                if (upper != otherUpper)
                    return upper > otherUpper ? 1 : -1;

                ushort lower = BinaryPrimitives.ReverseEndianness(
                    Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref thisRef, sizeof(ulong)))
                );
                ushort otherLower = BinaryPrimitives.ReverseEndianness(
                    Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref otherRef, sizeof(ulong)))
                );

                if (lower != otherLower)
                    return lower > otherLower ? 1 : -1;
            }
            else
            {
                ulong upper = Unsafe.ReadUnaligned<ulong>(ref thisRef);
                ulong otherUpper = Unsafe.ReadUnaligned<ulong>(ref otherRef);

                if (upper != otherUpper)
                    return upper > otherUpper ? 1 : -1;

                ushort lower = Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref thisRef, sizeof(ulong)));
                ushort otherLower = Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref otherRef, sizeof(ulong)));

                if (lower != otherLower)
                    return lower > otherLower ? 1 : -1;
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

            throw new ArgumentException($"Object must be of type {nameof(SnowflakeId)}", nameof(obj));
        }
    }
}
