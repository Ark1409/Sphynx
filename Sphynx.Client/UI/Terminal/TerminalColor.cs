// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.Client.Tui.Terminal
{
    public interface ITerminalColor
    {
        static ITerminalColor DefaultForeground => field ?? new TerminalDefaultColor();
        static ITerminalColor DefaultBackground => field ?? new TerminalDefaultColor();

        internal struct TerminalDefaultColor : ITerminalColor { }
    }
}
