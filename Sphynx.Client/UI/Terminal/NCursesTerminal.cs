// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.Client.Tui.Terminal
{
    public class NCursesTerminal : Terminal, IStreamTerminal
    {
        public override bool HasTrueColor => throw new NotImplementedException();

        public override bool HasMouseSupport => throw new NotImplementedException();

        public override bool HasGraphicsProtocol => throw new NotImplementedException();

        public override int Lines => throw new NotImplementedException();

        public override int Columns => throw new NotImplementedException();

        public override (int x, int y) CursorPosition { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void Clear()
        {
            throw new NotImplementedException();
        }

        public override void ClearLine()
        {
            throw new NotImplementedException();
        }

        public override void Erase(int count = 1)
        {
            throw new NotImplementedException();
        }

        public void Flush()
        {
            throw new NotImplementedException();
        }

        public override void MoveCursor(int dx, int dy)
        {
            throw new NotImplementedException();
        }

        public override TerminalAnsiColor NearestAnsiColor(TerminalTrueColor color)
        {
            throw new NotImplementedException();
        }

        public override int ReadKey(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public override void SetCursorColor(ITerminalColor color)
        {
            throw new NotImplementedException();
        }

        public override void SetCursorShape(CursorShape shape)
        {
            throw new NotImplementedException();
        }

        public override void SetCursorVisiblity(CursorVisibility visibility)
        {
            throw new NotImplementedException();
        }

        public override TerminalTrueColor TrueColorFor(TerminalAnsiColor color)
        {
            throw new NotImplementedException();
        }

        public override void Write(ColoredString str)
        {
            throw new NotImplementedException();
        }
    }
}
