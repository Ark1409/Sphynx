// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.Client.Tui.Terminal
{
    public readonly struct TerminalCellColor
    {
        public ITerminalColor Foreground { get; init; } = ITerminalColor.DefaultForeground;
        public ITerminalColor Background { get; init; } = ITerminalColor.DefaultBackground;

        public CellAttributes Attributes { get; init; } = CellAttributes.None;

        public TerminalCellColor() { }
        public TerminalCellColor(ITerminalColor fg) { Foreground = fg; }

        public enum CellAttributes
        {
            None = 0,
            Bold,
            Strikethrough,
            Underline,
            Italic,
            Reverse,

            // Supported only in certain terminals e.g. kitty
            // Default back to regular underline style if not avaiable
            DoubleUnderline,
            CurlyUnderline,
        }

        public static readonly TerminalCellColor Default = new()
        {
            Foreground = ITerminalColor.DefaultForeground,
            Background = ITerminalColor.DefaultBackground,
            Attributes = CellAttributes.None
        };

        public static TerminalCellColor Reset => Default;
    }
}
