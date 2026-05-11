// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.Client.Tui
{
    public interface ITerminalColor
    {
        private static ITerminalColor? _defaultColor;
        public static ITerminalColor DefaultColor => _defaultColor ??= new TerminalDefaultColor();

        internal sealed class TerminalDefaultColor : ITerminalColor { }
    }
}
