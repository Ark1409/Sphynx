// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.Client.Tui
{
    public readonly struct TerminalCellColor
    {
        public ITerminalColor Foreground { get; init; } = ITerminalColor.DefaultColor;
        public ITerminalColor Background { get; init; } = ITerminalColor.DefaultColor;

        public CellAttributes Attributes { get; init; } = CellAttributes.None;

        public TerminalCellColor() { }
        public TerminalCellColor(ITerminalColor fg) { Foreground = fg; }

        [Flags]
        public enum CellAttributes
        {
            None = 0x0,
            Bold = 0x1,
            Strikethrough = 0x2,
            Underline = 0x4,
            Italic = 0x8,
            Reverse = 0x10,
            Blink = 0x20,

            // Supported only in certain terminals e.g. kitty
            // Default back to regular underline style if not avaiable
            DoubleUnderline = 0x100,
            CurlyUnderline = 0x200,
            DottedUnderline = 0x400,
        }

        public static readonly TerminalCellColor Default = new()
        {
            Foreground = ITerminalColor.DefaultColor,
            Background = ITerminalColor.DefaultColor,
            Attributes = CellAttributes.None,
        };

        public static TerminalCellColor Reset => Default;
    }
}
