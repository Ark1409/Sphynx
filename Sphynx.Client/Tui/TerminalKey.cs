// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.Client.Tui
{
    public readonly struct TerminalKey
    {
        /// <summary>
        /// The 1-byte ASCII representation of the key, if available.
        /// </summary>
        public readonly byte? AsciiChar { get; init; }

        /// <summary>
        /// The Unicode code point for the key, if available.
        /// </summary>
        public readonly int? UnicodeChar { get; init; }
        public readonly SpecialKey? Key { get; init; }
        public readonly TerminalKeyModifiers Mods { get; init; }

        public TerminalKey(byte c, TerminalKeyModifiers mods = TerminalKeyModifiers.None)
        {
            AsciiChar = c;
            UnicodeChar = c;
            Mods = mods;
        }

        public TerminalKey(int c, TerminalKeyModifiers mods = TerminalKeyModifiers.None)
        {
            UnicodeChar = c;
            if (c is >= 0 and <= 0xff)
            {
                AsciiChar = (byte)c;
            }
            Mods = mods;
        }

        public TerminalKey(SpecialKey c, TerminalKeyModifiers mods = TerminalKeyModifiers.None)
        {
            Key = c;
            Mods = mods;
        }

        public bool HasModifiers(params TerminalKeyModifiers[] mods)
        {
            TerminalKeyModifiers aggr = mods.Aggregate(TerminalKeyModifiers.None, (a, c) => a |= c);
            return (Mods & aggr) == aggr;
        }

        public enum SpecialKey
        {
            Up,
            Down,
            Left,
            Right,
            PageUp,
            PageDown,
            Home,
            End,
            NumLock,
            Insert,
            Delete,
            PrintScreen,
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
        }
    }
}
