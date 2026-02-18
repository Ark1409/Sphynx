// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.Client.Tui
{
    [Flags]
    public enum TerminalKeyModifiers
    {
        Control = 0x1,
        Alt = 0x2,
        Shift = 0x4,
        Super = 0x8,
        None = 0x0
    }
}
