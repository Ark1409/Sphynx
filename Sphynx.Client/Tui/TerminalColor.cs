// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.Client.Tui
{
    public interface ITerminalColor
    {
        private static ITerminalColor? _defaultForeground;
        static ITerminalColor DefaultForeground => _defaultForeground ?? new TerminalDefaultColor();

        private static ITerminalColor? _defaultBackground;
        static ITerminalColor DefaultBackground => _defaultBackground ?? new TerminalDefaultColor();

        internal struct TerminalDefaultColor : ITerminalColor { }
    }
}
