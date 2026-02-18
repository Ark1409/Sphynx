// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.


namespace Sphynx.Client.Tui
{
    public class NCursesTerminal : Terminal
    {
        public override TerminalColorSupport ColorSupport => throw new NotImplementedException();

        public override bool HasMouseSupport => throw new NotImplementedException();

        public override bool HasGraphicsProtocol => throw new NotImplementedException();

        public override int Lines => throw new NotImplementedException();

        public override int Columns => throw new NotImplementedException();

        public override (int x, int y) CursorPosition { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override ITerminalColor CursorColor { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override TerminalCursorShape CursorShape { set => throw new NotImplementedException(); }
        public override TerminalCursorVisibility CursorVisibility { set => throw new NotImplementedException(); }

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

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override TerminalAnsiColor NearestAnsiColor(TerminalTrueColor color)
        {
            throw new NotImplementedException();
        }

        public override TerminalEvent PollEvent()
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

        protected internal override bool CanPollEvent(Type t)
        {
            throw new NotImplementedException();
        }
    }
}
