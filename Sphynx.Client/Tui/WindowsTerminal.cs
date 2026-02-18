// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.Client.Tui
{
    public abstract class WindowsTerminal : Terminal
    {
        public sealed override bool HasGraphicsProtocol => false;

        public override TerminalTrueColor TrueColorFor(TerminalAnsiColor color) => color.NearestTrueColor;
        public override TerminalAnsiColor NearestAnsiColor(TerminalTrueColor color) => color.NearestAnsiColor;

        private static WindowsTerminal? _standardTerminal;
        public static WindowsTerminal StandardTerminal => _standardTerminal ??= new StandardWindowsTerminal();
    }
}
