// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Sphynx.Utils;

namespace Sphynx.Client.Tui
{
    public struct Grapheme : IEquatable<Grapheme>
    {
        private const int LOCAL_LENGTH = 8;

        [InlineArray(LOCAL_LENGTH)]
        private struct RuneBuffer { private Rune _; }
        private RuneBuffer _runes;

        private Rune[]? _codePoints;
        private Span<Rune> InternalRunes => MemoryMarshal.CreateSpan(ref _runes[0], LOCAL_LENGTH);

        public ReadOnlySpan<Rune> Runes => Length > LOCAL_LENGTH ? _codePoints : InternalRunes[..Length];

        public int Length { get; }

        public Rune this[Index i] => Runes[i];
        public ReadOnlySpan<Rune> this[Range r] => Runes[r];

        public Grapheme(ReadOnlySpan<int> cps)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cps.Length);
            Span<Rune> r;
            if (cps.Length > LOCAL_LENGTH)
            {
                r = _codePoints = new Rune[cps.Length];
            }
            else
            {
                r = InternalRunes;
            }
            if (Unsafe.SizeOf<Rune>() == sizeof(int))
            {
                MemoryMarshal.AsBytes(cps).CopyTo(MemoryMarshal.AsBytes(r));
            }
            else
            {
                for (int i = 0; i < cps.Length; i++)
                {
                    r[i] = new(cps[i]);
                }
            }
            Length = cps.Length;
        }

        public Grapheme(ReadOnlySpan<Rune> cps)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cps.Length);
            Span<Rune> r;
            if (cps.Length > LOCAL_LENGTH)
            {
                r = _codePoints = new Rune[cps.Length];
            }
            else
            {
                r = InternalRunes;
            }
            cps.CopyTo(r);
            Length = cps.Length;
        }

        private string? _stringCache;

        public override string ToString()
        {
            if (_stringCache is not null) return _stringCache;
            return _stringCache = Runes.FromUtf32String();
        }

        public bool Equals(Grapheme other)
        {
            return other.Runes.SequenceEqual(Runes);
        }

        public override bool Equals(object? obj)
        {
            return obj is Grapheme grapheme && Equals(grapheme);
        }

        private HashCode? _hc;

        [MemberNotNull(nameof(_hc))]
        public override int GetHashCode()
        {
            if (_hc is null)
            {
                _hc = new();
                foreach (var r in Runes)
                    _hc.Value.Add(r);
            }
            return _hc.Value.ToHashCode();
        }

        public static bool operator ==(Grapheme left, Grapheme right) => left.Equals(right);

        public static bool operator !=(Grapheme left, Grapheme right) => !(left == right);
    }

    // TODO: Replace with sso'd List<T>
    public struct GraphemeBuilder
    {
        private const int LOCAL_LENGTH = 8;

        [InlineArray(LOCAL_LENGTH)]
        private struct RuneBuffer { private Rune _; }
        private RuneBuffer _runes;

        private Span<Rune> InternalRunes => MemoryMarshal.CreateSpan(ref _runes[0], LOCAL_LENGTH);
        public Span<Rune> UnsafeRunes => _codePoints is null ? InternalRunes[..Length] : CollectionsMarshal.AsSpan(_codePoints);

        private int _length = 0;
        private List<Rune>? _codePoints = null;
        public readonly int Length => _codePoints is null ? _length : _codePoints.Count;

        public GraphemeBuilder() { }

        public void RemoveLast() => RemoveAt(Length - 1);
        public void RemoveAt(int index)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Length);

            if (_codePoints is not null)
            {
                _codePoints.RemoveAt(index);
                return;
            }

            var runes = UnsafeRunes;
            runes[(index + 1)..].CopyTo(runes[index..]);
            runes[^1] = default;
            _length--;
        }

        public void RemoveRange(int index, int length)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfNegative(length);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(index + length, Length);

            if (_codePoints is not null)
            {
                _codePoints.RemoveRange(index, length);
                return;
            }

            var runes = UnsafeRunes;
            runes[(index + length)..].CopyTo(runes[index..]);
            runes[^length..].Clear();
            _length -= length;
        }

        public void RemoveRange(Range r)
        {
            var (off, len) = r.GetOffsetAndLength(Length);
            RemoveRange(off, len);
        }

        public void AddRange(ReadOnlySpan<Rune> r)
        {
            if (_codePoints is not null)
            {
                _codePoints.AddRange(r);
                return;
            }

            if (r.Length + _length > LOCAL_LENGTH)
            {
                _codePoints = new();
                _codePoints.AddRange(InternalRunes[.._length]);
                _codePoints.AddRange(r);
            }
            else
            {
                r.CopyTo(InternalRunes[_length..]);
                _length += r.Length;
            }
        }

        public void Add(Rune r)
        {
            if (_codePoints is not null)
            {
                _codePoints.Add(r);
                return;
            }

            if (_length < LOCAL_LENGTH)
            {
                InternalRunes[_length++] = r;
            }
            else
            {
                Debug.Assert(_length == LOCAL_LENGTH);
                _codePoints = new();
                _codePoints.AddRange(InternalRunes);
                _codePoints.Add(r);
            }
        }

        public Rune this[Index i] => UnsafeRunes[i];

        public Grapheme AsGrapheme() => new(UnsafeRunes);
    }

    public static class GraphemeUtils
    {
        /// <summary>
        /// Returns true if a grapheme cluster boundary is GUARANTEED after
        /// <paramref name="cp"/> regardless of what follows it.
        /// Returns false if the cluster might continue (you still need to look
        /// at the next code point to be certain).
        /// </summary>
        public static bool IsDefinitelyComplete(Rune cp)
        {
            // A cluster is definitely done after CR only if next is NOT LF -
            // but we can't see the next cp here, so CR is not definite.
            // Same for the emoji ZWJ case.
            // This method only returns true for cases where NO following
            // code point can extend the cluster.

            var gcb = Classify(cp);
            return gcb is
                GcbClass.Other or // plain letter/digit/symbol - nothing extends these
                GcbClass.Control or // always isolated
                GcbClass.LF or // GB4: break after LF
                GcbClass.LVT or // Hangul LVT - only T can follow, handled below
                GcbClass.T; // Hangul T - nothing follows T
            // Note: CR, L, LV, V, ExtendedPictographic, RegionalIndicator,
            //       Prepend are all NOT definite without lookahead.
        }

        /// <summary>
        /// Returns true if <paramref name="cp"/> is GUARANTEED to not be the
        /// last code point in its cluster - i.e. at least one more code point
        /// must follow before the cluster can end.
        /// </summary>
        public static bool IsDefinitelyIncomplete(Rune cp)
        {
            return Classify(cp) == GcbClass.Prepend;
            // Only Prepend is unconditionally guaranteed to have a follower.
            // ZWJ and CR are conditional (depend on what follows).
        }

        /// <summary>
        /// Returns true if the cluster represented by <paramref name="current"/>
        /// is DEFINITELY incomplete - i.e. at least one more code point must
        /// follow before the cluster can end.
        ///
        /// Cases:
        ///   1. Last code point is Prepend - GB9b unconditionally requires a follower.
        ///   2. Last code point is CR      - GB3 requires LF to follow.
        ///   3. Last code point is ZWJ and history is ExtPic Extend*
        ///                                 - GB11 is armed, next ExtPic would extend.
        ///   4. Odd number of Regional Indicators
        ///                                 - GB12/13, one more RI would extend.
        ///
        /// Note: case 3 and 4 are "definitely incomplete" only in the sense that
        /// a boundary-free continuation EXISTS - a non-ExtPic after an armed ZWJ,
        /// or a non-RI after an odd RI run, would still end the cluster. If you
        /// want "a boundary CANNOT occur next" then only cases 1 and 2 qualify.
        /// The <paramref name="strict"/> flag lets you choose.
        /// </summary>
        public static bool IsIncomplete(ReadOnlySpan<Rune> current, bool strict = false)
        {
            if (current.IsEmpty) return false;

            Rune last = current[^1];
            GcbClass lastClass = Classify(last);

            // GB9b: Prepend x Any - a boundary can NEVER follow a Prepend.
            if (lastClass == GcbClass.Prepend) return true;

            // GB3: CR x LF - if CR is the last code point, LF would extend it.
            // A non-LF would break, so this is only "definitely" incomplete in the
            // loose sense. Excluded in strict mode.
            if (!strict && lastClass == GcbClass.CR) return true;

            // If the cluster ends with ZWJ and history is ExtPic Extend*, then
            // the next ExtPic would be absorbed. Excluded in strict mode because
            // a non-ExtPic after ZWJ would end the cluster.
            if (!strict && lastClass == GcbClass.ZWJ && IsEmojiZwjArmed(current))
                return true;

            // GB12/13: an odd count of RIs means one more RI would extend.
            // Excluded in strict mode because a non-RI ends the cluster.
            if (!strict && lastClass == GcbClass.RegionalIndicator && HasOddRI(current))
                return true;

            return false;
        }

        /// <summary>
        /// Returns true if the cluster matches ExtPic Extend* ZWJ -
        /// i.e. GB11 is armed and the next ExtPic would be absorbed.
        /// Walks backwards from the ZWJ (already confirmed by caller).
        /// </summary>
        private static bool IsEmojiZwjArmed(ReadOnlySpan<Rune> current)
        {
            // Walk backwards past any Extend characters looking for an ExtPic root.
            // current[^1] is already confirmed ZWJ by caller.
            for (int i = current.Length - 2; i >= 0; i--)
            {
                var cls = Classify(current[i]);
                if (cls == GcbClass.ExtendedPictographic) return true;
                if (cls != GcbClass.Extend) return false;
            }
            return false;
        }

        /// <summary>
        /// Returns true if the cluster contains an odd number of consecutive
        /// Regional Indicator code points at its tail.
        /// </summary>
        private static bool HasOddRI(ReadOnlySpan<Rune> current)
        {
            int count = 0;
            for (int i = current.Length - 1; i >= 0; i--)
            {
                if (Classify(current[i]) == GcbClass.RegionalIndicator)
                    count++;
                else
                    break;
            }
            return count % 2 == 1;
        }


        public enum GcbClass
        {
            Other,
            CR, LF, Control,
            Extend, ZWJ, SpacingMark, Prepend,
            L, V, T, LV, LVT,
            RegionalIndicator,
            ExtendedPictographic,
        }

        public static GcbClass Classify(Rune r)
        {
            int v = r.Value;

            if (v == 0x000D) return GcbClass.CR;
            if (v == 0x000A) return GcbClass.LF;
            if (v == 0x200D) return GcbClass.ZWJ;

            if (v is >= 0x1F1E6 and <= 0x1F1FF) return GcbClass.RegionalIndicator;

            if (v is (>= 0x1100 and <= 0x115F) or (>= 0xA960 and <= 0xA97C))
                return GcbClass.L;
            if (v is (>= 0x1160 and <= 0x11A7) or (>= 0xD7B0 and <= 0xD7C6))
                return GcbClass.V;
            if (v is (>= 0x11A8 and <= 0x11FF) or (>= 0xD7CB and <= 0xD7FB))
                return GcbClass.T;
            if (v is >= 0xAC00 and <= 0xD7A3)
                return (v - 0xAC00) % 28 == 0 ? GcbClass.LV : GcbClass.LVT;

            if (v is (>= 0xFE00 and <= 0xFE0F) or (>= 0xE0100 and <= 0xE01EF))
                return GcbClass.Extend;

            if (IsExtendedPictographic(v)) return GcbClass.ExtendedPictographic;

            if (IsPrepend(v)) return GcbClass.Prepend;

            return Rune.GetUnicodeCategory(r) switch
            {
                UnicodeCategory.Control => GcbClass.Control,
                UnicodeCategory.Surrogate => GcbClass.Control,
                UnicodeCategory.NonSpacingMark => GcbClass.Extend,
                UnicodeCategory.EnclosingMark => GcbClass.Extend,
                UnicodeCategory.SpacingCombiningMark => GcbClass.SpacingMark,
                UnicodeCategory.Format => GcbClass.Extend, // non-Prepend Cf
                _ => GcbClass.Other,
            };
        }

        // Source: GraphemeBreakProperty.txt - GCB=Prepend
        private static bool IsPrepend(int v) => v is
            (>= 0x0600 and <= 0x0605) or // Arabic number signs (Cf)
            0x06DD or // Arabic end of ayah (Cf)
            0x070F or // Syriac abbreviation mark (Cf)
            0x0890 or 0x0891 or // Arabic pound/piastre mark (Cf)
            0x08E2 or // Arabic disputed end of ayah (Cf)
            0x0D4E or // Malayalam letter dot reph (Lo) <- not Cf!
            0x110BD or // Kaithi number sign (Cf)
            0x110CD or // Kaithi number sign above (Cf)
            (>= 0x111C2 and <= 0x111C3) or // Sharada sign (Lo) <- not Cf!
            0x1193F or // Dives Akuru prefixed nasal (Lo) <- not Cf!
            0x11941 or // Dives Akuru initial ra (Lo) <- not Cf!
            0x11A3A or // Zanabazar square cluster-initial (Lo) <- not Cf!
            (>= 0x11A84 and <= 0x11A89) or // Zanabazar square initial (Lo) <- not Cf!
            0x11D46 or // Masaram Gondi repha (Lo) <- not Cf!
            0x11D8A or // Gunjala Gondi (Lo) <- not Cf!
            (>= 0x11F02 and <= 0x11F03); // Kawi (Lo/Cf)

        // Source: emoji-data.txt - Emoji_Presentation + Extended_Pictographic
        // General Category alone (mostly So) cannot identify these.
        private static bool IsExtendedPictographic(int v) => v is
            0x00A9 or 0x00AE or
            (>= 0x203C and <= 0x2049) or
            0x2122 or 0x2139 or
            (>= 0x2194 and <= 0x2199) or
            (>= 0x21A9 and <= 0x21AA) or
            (>= 0x231A and <= 0x231B) or
            0x2328 or 0x23CF or
            (>= 0x23E9 and <= 0x23F3) or
            (>= 0x23F8 and <= 0x23FA) or
            0x24C2 or
            (>= 0x25AA and <= 0x25AB) or
            0x25B6 or 0x25C0 or
            (>= 0x25FB and <= 0x25FE) or
            (>= 0x2600 and <= 0x2605) or
            (>= 0x2607 and <= 0x2612) or
            (>= 0x2614 and <= 0x2685) or
            (>= 0x2690 and <= 0x2705) or
            (>= 0x2708 and <= 0x2712) or
            0x2714 or 0x2716 or 0x271D or 0x2721 or
            (>= 0x2733 and <= 0x2734) or
            0x2744 or 0x2747 or 0x274C or 0x274E or
            (>= 0x2753 and <= 0x2755) or
            0x2757 or
            (>= 0x2763 and <= 0x2767) or
            (>= 0x2795 and <= 0x2797) or
            0x27A1 or 0x27B0 or 0x27BF or
            (>= 0x2934 and <= 0x2935) or
            (>= 0x2B05 and <= 0x2B07) or
            (>= 0x2B1B and <= 0x2B1C) or
            0x2B50 or 0x2B55 or
            0x3030 or 0x303D or 0x3297 or 0x3299 or
            (>= 0x1F000 and <= 0x1F0FF) or
            (>= 0x1F100 and <= 0x1F1FF) or
            (>= 0x1F200 and <= 0x1F2FF) or
            (>= 0x1F300 and <= 0x1F5FF) or
            (>= 0x1F600 and <= 0x1F64F) or
            (>= 0x1F650 and <= 0x1F67F) or
            (>= 0x1F680 and <= 0x1F6FF) or
            (>= 0x1F700 and <= 0x1F77F) or
            (>= 0x1F780 and <= 0x1F7FF) or
            (>= 0x1F800 and <= 0x1F8FF) or
            (>= 0x1F900 and <= 0x1F9FF) or
            (>= 0x1FA00 and <= 0x1FA6F) or
            (>= 0x1FA70 and <= 0x1FAFF);
    }
}
