// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using MessagePack;

namespace Sphynx.Utils
{
    public static class MessagePackExtensions
    {
        public static bool TryReadByte(this ref MessagePackReader reader, out byte value)
        {
            return reader.TryReadByte(out value, out _);
        }

        public static bool TryReadByte(this ref MessagePackReader reader, out byte value, out MessagePackPrimitives.DecodeResult result)
        {
            result = MessagePackPrimitives.TryReadByte(reader.Sequence.FirstSpan, out value, out int tokenSize);

            if (result is MessagePackPrimitives.DecodeResult.InsufficientBuffer or MessagePackPrimitives.DecodeResult.EmptyBuffer)
            {
                var sequence = reader.Sequence;

                if (sequence.Length < tokenSize)
                    return false;

                Span<byte> buffer = tokenSize <= 32 ? stackalloc byte[32] : new byte[tokenSize];

                if (sequence.Slice(0, tokenSize).TryCopyTo(buffer))
                    result = MessagePackPrimitives.TryReadByte(buffer, out value, out tokenSize);
            }

            if (result != MessagePackPrimitives.DecodeResult.Success)
                return false;

            reader.Skip();
            return true;
        }

        public static bool TryReadUInt16(this ref MessagePackReader reader, out ushort value)
        {
            return reader.TryReadUInt16(out value, out _);
        }

        public static bool TryReadUInt16(this ref MessagePackReader reader, out ushort value, out MessagePackPrimitives.DecodeResult result)
        {
            result = MessagePackPrimitives.TryReadUInt16(reader.Sequence.FirstSpan, out value, out int tokenSize);

            if (result is MessagePackPrimitives.DecodeResult.InsufficientBuffer or MessagePackPrimitives.DecodeResult.EmptyBuffer)
            {
                var sequence = reader.Sequence;

                if (sequence.Length < tokenSize)
                    return false;

                Span<byte> buffer = tokenSize <= 32 ? stackalloc byte[32] : new byte[tokenSize];

                if (sequence.Slice(0, tokenSize).TryCopyTo(buffer))
                    result = MessagePackPrimitives.TryReadUInt16(buffer, out value, out tokenSize);
            }

            if (result != MessagePackPrimitives.DecodeResult.Success)
                return false;

            reader.Skip();
            return true;
        }

        public static bool TryReadString(this ref MessagePackReader reader, out string? str)
        {
            return reader.TryReadString(out str, out _);
        }

        public static bool TryReadString(this ref MessagePackReader reader, out string? str, out MessagePackPrimitives.DecodeResult result)
        {
            result = MessagePackPrimitives.TryReadStringHeader(reader.Sequence.FirstSpan, out uint strLen, out int tokenSize);
            str = null;

            if (result is MessagePackPrimitives.DecodeResult.InsufficientBuffer or MessagePackPrimitives.DecodeResult.EmptyBuffer)
            {
                var sequence = reader.Sequence;

                if (sequence.Length < tokenSize)
                    return false;

                Span<byte> buffer = tokenSize <= 32 ? stackalloc byte[32] : new byte[tokenSize];

                if (sequence.Slice(0, tokenSize).TryCopyTo(buffer))
                    result = MessagePackPrimitives.TryReadStringHeader(buffer, out strLen, out tokenSize);
            }

            if (result != MessagePackPrimitives.DecodeResult.Success)
                return false;

            if (reader.Sequence.Length - tokenSize < strLen)
            {
                result = MessagePackPrimitives.DecodeResult.InsufficientBuffer;
                return false;
            }

            str = reader.ReadString();
            return true;
        }

        public static bool TryReadString(this ref MessagePackReader reader, ReadOnlySpan<byte> u8Str)
        {
            return reader.TryReadString(u8Str, out _);
        }

        public static bool TryReadString(this ref MessagePackReader reader, ReadOnlySpan<byte> u8Str, out MessagePackPrimitives.DecodeResult result)
        {
            result = MessagePackPrimitives.TryReadStringHeader(reader.Sequence.FirstSpan, out uint strLen, out int tokenSize);

            if (result is MessagePackPrimitives.DecodeResult.InsufficientBuffer or MessagePackPrimitives.DecodeResult.EmptyBuffer)
            {
                var sequence = reader.Sequence;

                if (sequence.Length < tokenSize)
                    return false;

                Span<byte> buffer = tokenSize <= 32 ? stackalloc byte[32] : new byte[tokenSize];

                if (sequence.Slice(0, tokenSize).TryCopyTo(buffer))
                    result = MessagePackPrimitives.TryReadStringHeader(buffer, out strLen, out tokenSize);
            }

            if (result != MessagePackPrimitives.DecodeResult.Success || strLen != u8Str.Length)
                return false;

            if (reader.Sequence.Length - tokenSize < strLen)
            {
                result = MessagePackPrimitives.DecodeResult.InsufficientBuffer;
                return false;
            }

            var tempReader = reader.CreatePeekReader();

            if (tempReader.TryReadStringSpan(out var stringSpan))
            {
                if (!stringSpan.SequenceEqual(u8Str))
                    return false;

                reader.Skip();
                return true;
            }

            var stringSeq = tempReader.ReadStringSequence()!.Value;

            if (new SequenceReader<byte>(stringSeq).IsNext(u8Str))
            {
                reader.Skip();
                return true;
            }

            return false;
        }

        public static bool TryReadMapHeader(this ref MessagePackReader reader, out int count)
        {
            return reader.TryReadMapHeader(out count, out _);
        }

        public static bool TryReadMapHeader(this ref MessagePackReader reader, out int count, out MessagePackPrimitives.DecodeResult result)
        {
            result = MessagePackPrimitives.TryReadMapHeader(reader.Sequence.FirstSpan, out uint mapCount, out int tokenSize);
            count = unchecked((int)mapCount);

            if (result is MessagePackPrimitives.DecodeResult.InsufficientBuffer or MessagePackPrimitives.DecodeResult.EmptyBuffer)
            {
                var sequence = reader.Sequence;

                if (sequence.Length < tokenSize)
                    return false;

                Span<byte> buffer = tokenSize <= 32 ? stackalloc byte[32] : new byte[tokenSize];

                if (sequence.Slice(0, tokenSize).TryCopyTo(buffer))
                {
                    result = MessagePackPrimitives.TryReadMapHeader(buffer, out mapCount, out tokenSize);
                    count = unchecked((int)mapCount);
                }
            }

            if (result != MessagePackPrimitives.DecodeResult.Success)
                return false;

            if (mapCount > int.MaxValue)
                return false;

            reader.Skip();
            return true;
        }
    }
}
