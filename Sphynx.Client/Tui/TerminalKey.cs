// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Text;

namespace Sphynx.Client.Tui
{
    public struct TerminalKey : IEquatable<TerminalKey>
    {
        public readonly TerminalKeyModifiers Mods { get; init; }

        /// <summary>
        /// The entire grapheme cluster represented by this key, if available.
        /// </summary>
        public readonly Grapheme? Grapheme { get; }

        /// <summary>
        /// If this key is not representable as a grapheme or code point, then this stores the resulting key.
        /// </summary>
        public readonly SpecialKey? Key { get; }

        /// <summary>
        /// The Unicode code point for the key, if available.
        /// </summary>
        public readonly Rune? UnicodeChar
        {
            get
            {
                if (Grapheme is null) return null;
                if (Grapheme.Value.Length != 1) return null;
                return Grapheme.Value[0];
            }
        }

        private byte? _asciiCharCache;
        public byte? AsciiChar
        {
            get
            {
                if (_asciiCharCache is { } ch) return ch;
                if (UnicodeChar is not { } unicodeCh || !unicodeCh.IsAscii) return null;
                Span<byte> b = stackalloc byte[1];
                var count = unicodeCh.EncodeToUtf8(b);
                Debug.Assert(count == 1);
                return _asciiCharCache = b[0];
            }
        }

        private string? _keyStringCache;
        public string? KeyString
        {
            get
            {
                if (_keyStringCache is not null) return _keyStringCache;
                if (Grapheme is not null) _keyStringCache = Grapheme.Value.ToString();
                return _keyStringCache;
            }
        }


        public TerminalKey(byte c, TerminalKeyModifiers mods = TerminalKeyModifiers.None) : this(new Rune(c), mods)
        {
        }

        public TerminalKey(char c, TerminalKeyModifiers mods = TerminalKeyModifiers.None) : this(new Rune(c), mods)
        {

        }

        public TerminalKey(int c, TerminalKeyModifiers mods = TerminalKeyModifiers.None) : this(new Rune(c), mods)
        {
        }

        public TerminalKey(Rune r, TerminalKeyModifiers mods = TerminalKeyModifiers.None) : this(new Grapheme([r]), mods)
        {
        }

        public TerminalKey(in Grapheme g, TerminalKeyModifiers mods = TerminalKeyModifiers.None)
        {
            Grapheme = g;
            Mods = mods;
        }

        public TerminalKey(SpecialKey c, TerminalKeyModifiers mods = TerminalKeyModifiers.None)
        {
            Key = c;
            Mods = mods;
        }

        public readonly bool HasModifiers(TerminalKeyModifiers mods)
        {
            return (Mods & mods) == mods;
        }

        public bool HasModifiers(params TerminalKeyModifiers[] mods)
        {
            TerminalKeyModifiers aggr = mods.Aggregate(TerminalKeyModifiers.None, (a, c) => a |= c);
            return HasModifiers(aggr);
        }

        public enum SpecialKey
        {
            Up,
            Down,
            Left,
            Right,
            PageUp,
            PgUp = PageUp,
            PageDown,
            PgDn = PageDown,
            Home,
            Begin,
            End,
            NumLock,
            Insert,
            Delete,
            PrintScreen,
            F0,
            F1,
            F2,
            F3,
            F4,
            F5,
            F6,
            F7,
            F8,
            F9,
            F10,
            F11,
            F12,
            F13,
            F14,
            F15,
            F16,
            F17,
            F18,
            F19,
            F20,
            F21,
            F22,
            F23,
            F24,
            Pause,
        }
        public readonly bool Equals(TerminalKey other)
        {
            bool b = Key == other.Key && Mods == other.Mods;
            b &= other.Grapheme is null == Grapheme is null;
            if (!b) return false;
            if (Grapheme is not null) b &= Grapheme.Value.Equals(other.Grapheme!.Value);
            return b;
        }

        public readonly override bool Equals(object? obj) => obj is TerminalKey key && Equals(key);

        public static bool operator ==(TerminalKey left, TerminalKey right) => left.Equals(right);
        public static bool operator !=(TerminalKey left, TerminalKey right) => !(left == right);

        public readonly override int GetHashCode()
        {
            if (Key is { } k) return (int)k;
            else return 256 + Grapheme.GetHashCode();
        }
    }
}
