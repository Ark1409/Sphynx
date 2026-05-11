// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Text;

namespace Sphynx.Utils
{
    public static class SpanExtensions
    {
        public static T? Max<T>(this ReadOnlySpan<T> span) where T : struct, IComparable<T> => Max(span, Comparer<T>.Default);
        public static T? Max<T>(this ReadOnlySpan<T> span, IComparer<T> cmp) where T : struct
        {
            if (span.Length <= 0) return null;
            var max = span[0];
            for (int i = 1; i < span.Length; i++)
            {
                var val = span[i];
                int ret = cmp.Compare(max, val);
                if (ret < 0) max = val;
            }
            return max;
        }

        public static T? Max<T>(this Span<T> span) where T : struct, IComparable<T> => Max((ReadOnlySpan<T>)span);
        public static T? Max<T>(this Span<T> span, IComparer<T> cmp) where T : struct => Max((ReadOnlySpan<T>)span, cmp);

        public static int FromUtf32String(this ReadOnlySpan<Rune> sp, Span<char> output)
        {
            if (sp.Length <= 0) return 0;
            var count = 0;
            for (int i = 0; i < sp.Length; i++)
            {
                var r = sp[i];
                var seqLen = r.Utf16SequenceLength;
                if (count + seqLen > output.Length) throw new ArgumentException();
                if (seqLen == 1)
                {
                    output[count++] = (char)r.Value;
                }
                else
                {
                    count += r.EncodeToUtf16(output[count..]);
                }
            }
            return count;
        }

        public static string FromUtf32String(this ReadOnlySpan<Rune> sp)
        {
            if (sp.Length <= 0) return string.Empty;
            var sb = new StringBuilder(sp.Length * 2);
            for (int i = 0; i < sp.Length; i++)
            {
                sb.Append(sp[i].ToString());
            }
            return sb.ToString();
        }

        public static string FromUtf32String(this Span<Rune> sp) => FromUtf32String((ReadOnlySpan<Rune>)sp);
        public static string FromUtf32String(this ReadOnlyMemory<Rune> sp) => FromUtf32String(sp.Span);
        public static string FromUtf32String(this Memory<Rune> sp) => FromUtf32String(sp.Span);
        public static int FromUtf32String(this Span<Rune> sp, Span<char> output) => FromUtf32String((ReadOnlySpan<Rune>)sp, output);
        public static int FromUtf32String(this ReadOnlyMemory<Rune> sp, Span<char> output) => FromUtf32String(sp.Span, output);
        public static int FromUtf32String(this Memory<Rune> sp, Span<char> output) => FromUtf32String(sp.Span, output);

        public static string FromUtf32String(this ReadOnlySpan<int> sp)
        {
            if (sp.Length <= 0) return string.Empty;
            var sb = new StringBuilder(sp.Length * 2);
            for (int i = 0; i < sp.Length; i++)
            {
                if (sp[i] <= 0x7f)
                {
                    sb.Append((char)sp[i]);
                }
                else
                {
                    sb.Append(char.ConvertFromUtf32(sp[i]));
                }
            }
            return sb.ToString();
        }

        public static int FromUtf32String(this ReadOnlySpan<int> sp, Span<char> output)
        {
            if (sp.Length <= 0) return 0;
            var count = 0;
            for (int i = 0; i < sp.Length; i++)
            {
                var r = new Rune(sp[i]);
                var seqLen = r.Utf16SequenceLength;
                if (count + seqLen > output.Length) throw new ArgumentException();
                if (seqLen == 1)
                {
                    output[count++] = (char)r.Value;
                }
                else
                {
                    count += r.EncodeToUtf16(output[count..]);
                }
            }
            return count;
        }

        public static string FromUtf32String(this Span<int> sp) => FromUtf32String((ReadOnlySpan<int>)sp);
        public static string FromUtf32String(this ReadOnlyMemory<int> sp) => FromUtf32String(sp.Span);
        public static string FromUtf32String(this Memory<int> sp) => FromUtf32String(sp.Span);
        public static int FromUtf32String(this Span<int> sp, Span<char> output) => FromUtf32String((ReadOnlySpan<int>)sp, output);
        public static int FromUtf32String(this ReadOnlyMemory<int> sp, Span<char> output) => FromUtf32String(sp.Span, output);
        public static int FromUtf32String(this Memory<int> sp, Span<char> output) => FromUtf32String(sp.Span, output);
    }
}
