// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Sphynx.Core
{
    public readonly partial struct SnowflakeId : ISpanFormattable
    {
        private const int HEX_LENGTH = 20;
        private const int DECIMAL_LENGTH = 25;

        private uint SequenceMachine
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ref byte smRef = ref Unsafe.AsRef(_sm0);
                uint sm = Unsafe.ReadUnaligned<uint>(ref smRef);
                return BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(sm) : sm;
            }
        }

        /// <summary>
        /// Attempts to create a new <see cref="SnowflakeId"/> from the input string value.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="snowflakeId">The resulting <see cref="SnowflakeId"/>.</param>
        /// <returns>true if the parse operation was successful; false otherwise.</returns>
        public static bool TryParse(ReadOnlySpan<char> input, [NotNullWhen(true)] out SnowflakeId? snowflakeId)
        {
            if (input.Length == HEX_LENGTH)
            {
                if (!long.TryParse(input[..12], NumberStyles.AllowHexSpecifier, null, out long timestamp) ||
                    !ushort.TryParse(input[12..16], NumberStyles.AllowHexSpecifier, null, out ushort s) ||
                    !ushort.TryParse(input[16..20], NumberStyles.AllowHexSpecifier, null, out ushort m))
                {
                    snowflakeId = null;
                    return false;
                }

                snowflakeId = new SnowflakeId(timestamp, s, m);
                return true;
            }

            if (input.Length == DECIMAL_LENGTH)
            {
                if (!long.TryParse(input[..15], NumberStyles.None, null, out long timestamp) ||
                    !uint.TryParse(input[15..25], NumberStyles.None, null, out uint sm))
                {
                    snowflakeId = null;
                    return false;
                }

                snowflakeId = new SnowflakeId(timestamp, (ushort)(sm >> 16), (ushort)(sm & 0xFFFF));
                return true;
            }

            snowflakeId = null;
            return false;
        }

        /// <summary>
        /// Returns a hexadecimal representation of this <see cref="SnowflakeId"/>.
        /// </summary>
        /// <returns>A hexadecimal representation of this <see cref="SnowflakeId"/>.</returns>
        public override string ToString()
        {
            string hex = string.Create(HEX_LENGTH, this, static (span, inst) =>
            {
                bool formatted = inst.TryFormat(span, out int charsWritten);

                Debug.Assert(formatted);
                Debug.Assert(charsWritten == span.Length);
            });

            return hex;
        }

        public string ToString(string? format, IFormatProvider? formatProvider = null)
        {
            if (string.IsNullOrEmpty(format))
            {
                goto DefaultFormat;
            }

            if (format[0] == 'D' || format[0] == 'd')
            {
                return string.Create(DECIMAL_LENGTH, this, static (span, inst) =>
                {
                    bool formatted = inst.TryFormatDecimal(span, out int charsWritten);

                    Debug.Assert(formatted);
                    Debug.Assert(charsWritten == span.Length);
                });
            }

            if (format[0] == 'X')
            {
                return string.Create(HEX_LENGTH, this, static (span, inst) =>
                {
                    bool formatted = inst.TryFormatHex(span, out int charsWritten, lowerCase: false);

                    Debug.Assert(formatted);
                    Debug.Assert(charsWritten == span.Length);
                });
            }

            DefaultFormat:
            {
                return string.Create(HEX_LENGTH, this, static (span, inst) =>
                {
                    bool formatted = inst.TryFormatHex(span, out int charsWritten);

                    Debug.Assert(formatted);
                    Debug.Assert(charsWritten == span.Length);
                });
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryFormat(Span<char> destination, out int charsWritten, IFormatProvider? provider = null)
        {
            return TryFormatHex(destination, out charsWritten);
        }

        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider = null)
        {
            if (format.IsEmpty)
            {
                charsWritten = 0;
                return false;
            }

            if (format[0] == 'D' || format[0] == 'd')
            {
                return TryFormatDecimal(destination, out charsWritten);
            }

            return TryFormatHex(destination, out charsWritten, format[0] != 'X');
        }

        private bool TryFormatHex(Span<char> destination, out int charsWritten, bool lowerCase = true)
        {
            if (destination.Length < HEX_LENGTH)
            {
                charsWritten = 0;
                return false;
            }

            ulong timestamp = Unsafe.As<long, ulong>(ref Unsafe.AsRef(Timestamp));
            uint sm = SequenceMachine;

            if (!lowerCase)
            {
                bool formatted = timestamp.TryFormat(destination, out charsWritten, format: "X12");
                formatted &= sm.TryFormat(destination[charsWritten..], out int smCharsWritten, format: "X8");

                Debug.Assert(formatted);

                charsWritten += smCharsWritten;

                Debug.Assert(charsWritten == HEX_LENGTH);

                return true;
            }

            // Default format
            {
                bool formatted = timestamp.TryFormat(destination, out charsWritten, format: "x12");
                formatted &= sm.TryFormat(destination[charsWritten..], out int smCharsWritten, format: "x8");

                Debug.Assert(formatted);

                charsWritten += smCharsWritten;

                Debug.Assert(charsWritten == HEX_LENGTH);

                return true;
            }
        }

        private bool TryFormatDecimal(Span<char> destination, out int charsWritten)
        {
            if (destination.Length < DECIMAL_LENGTH)
            {
                charsWritten = 0;
                return false;
            }

            ulong timestamp = Unsafe.As<long, ulong>(ref Unsafe.AsRef(Timestamp));
            uint sm = SequenceMachine;

            bool formatted = timestamp.TryFormat(destination, out charsWritten, format: "D15");
            formatted &= sm.TryFormat(destination[charsWritten..], out int smCharsWritten, format: "D10");

            Debug.Assert(formatted);

            charsWritten += smCharsWritten;

            Debug.Assert(charsWritten == DECIMAL_LENGTH);

            return true;
        }
    }
}
