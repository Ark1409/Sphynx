using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Sphynx.Client.Utils;

[assembly: InternalsVisibleTo("Sphynx.Test")]

namespace Sphynx.Client.UI
{
    internal class GapBuffer
    {
        public const int DEFAULT_GAP_SIZE = 16;

        public ReadOnlyCollection<char> Buffer => _buffer.AsReadOnly();

        public int Count => _buffer.Count;

        public int GapBegin { get; private set; }

        public int GapEnd { get; private set; }

        public int GapSize => GapEnd - GapBegin + 1;

        public string Text
        {
            get
            {
                var sb = new StringBuilder(GapBegin + _buffer.Count - GapEnd);
                var listSpan = CollectionsMarshal.AsSpan(_buffer);
                foreach (char c in listSpan[..GapBegin])
                {
                    sb.Append(c);
                }

                foreach (char c in listSpan[(GapEnd + 1)..])
                {
                    sb.Append(c);
                }
                return sb.ToString();
            }
        }

        private readonly List<char> _buffer;

        public GapBuffer(int initialCapacity = DEFAULT_GAP_SIZE)
        {
            initialCapacity = Math.Max(1, initialCapacity);
            _buffer = new List<char>(initialCapacity);
            _buffer.Resize(initialCapacity);
            GapBegin = 0;
            GapEnd = initialCapacity - 1;
        }

        public GapBuffer(string initialText, int initialCapacity) : this(Math.Max(initialText.Length, initialCapacity)) { InsertText(initialText); }

        public GapBuffer(string initialText) : this(initialText, initialText.Length * 2) { }

        public GapBuffer InsertText(string str)
        {
            if (str.Length > GapSize) ResizeGap(str.Length + GapSize * 2);

            unsafe
            {
                fixed (char* bufferPtr = CollectionsMarshal.AsSpan(_buffer))
                fixed (char* strPtr = str)
                {
                    System.Buffer.MemoryCopy(strPtr, bufferPtr + GapBegin, str.Length * sizeof(char), str.Length * sizeof(char));
                }
            }

            GapBegin += str.Length;
            return this;
        }

        public unsafe GapBuffer Move(int count)
        {
            count = Math.Clamp(count, -GapBegin, _buffer.Count - GapEnd - 1);

            fixed (char* ptr = CollectionsMarshal.AsSpan(_buffer))
            {
                if (count > 0) { System.Buffer.MemoryCopy(ptr + GapEnd + 1, ptr + GapBegin, count * sizeof(char), count * sizeof(char)); }
                else { System.Buffer.MemoryCopy(ptr + GapBegin + count, ptr + GapEnd + 1 + count, -count * sizeof(char), -count * sizeof(char)); }
            }

            GapBegin += count;
            GapEnd += count;
            return this;
        }

        public GapBuffer MoveAbs(int index) => Move(-(GapBegin - index));

        public unsafe void ResizeGap(int gapSize)
        {
            if (gapSize < 0) throw new ArgumentOutOfRangeException(nameof(gapSize), "Gap size must be greater than 0");

            int copyCount = _buffer.Count - GapEnd - 1;

            if (gapSize > GapSize)
            {
                int gapDelta = gapSize - GapSize;
                _buffer.Grow(Math.Abs(gapDelta));

                if (copyCount > 0)
                {
                    fixed (char* ptr = CollectionsMarshal.AsSpan(_buffer))
                    {
                        System.Buffer.MemoryCopy(ptr + GapEnd + 1, ptr + GapEnd + 1 + gapDelta, copyCount * sizeof(char), copyCount * sizeof(char));
                    }
                }
            }
            else
            {
                int gapDelta = GapSize - gapSize;
                if (copyCount > 0)
                {
                    fixed (char* ptr = CollectionsMarshal.AsSpan(_buffer))
                    {
                        System.Buffer.MemoryCopy(ptr + GapEnd + 1, ptr + GapBegin + gapSize, copyCount * sizeof(char), copyCount * sizeof(char));
                    }
                }
                _buffer.Shrink(Math.Abs(gapDelta));
            }

            GapEnd = GapBegin + gapSize - 1;
        }

        /// <inheritdoc/>
        public override string ToString() => Text;

        public static GapBuffer operator +(GapBuffer b, string str) => b.InsertText(str);

        public static GapBuffer operator +(GapBuffer b, int count) => b.Move(count);

        public static GapBuffer operator -(GapBuffer b, int count) => b + (-count);

        public static explicit operator string(GapBuffer b) => b.Text;
    }
}
