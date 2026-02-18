// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Client.Tui;

namespace Sphynx.Client
{
    internal static class Program
    {
        // static int Main(string[] args)
        // {
        //     var scr = NCurses.InitScreen();
        //     Console.WriteLine("Hello World");
        //     NCurses.WindowAddString(scr, "Hello World");
        //     NCurses.Refresh();
        //     NCurses.Raw();
        //     NCurses.GetChar();
        //     NCurses.EndWin();
        //     return 0;
        // }

        private static int Main(string[] args)
        {
            var term = WindowsTerminal.StandardTerminal;
            var poller = new TerminalEventPoller(term);

            bool shouldRun = true;
            while (shouldRun)
            {
                var ev = poller.PollEvent();
                if (ev.WindowEvent is not null)
                {
                    var wv = ev.WindowEvent.Value;
                    term.WriteLine($"Console was resized to {wv.NewSize}");
                }
                else if (ev.KeyEvent is not null)
                {
                    var kv = ev.KeyEvent.Value;
                    var str = kv.Key.AsciiChar is null ? "INVALID" : kv.Key.AsciiChar.Value.ToString();
                    term.WriteLine($"Just got your key: {str}");

                    if (kv.Key.AsciiChar == 'c' && kv.Key.HasModifiers(TerminalKeyModifiers.Control))
                    {
                        term.WriteLine($"Exiting...");
                        shouldRun = false;
                    }
                }
            }
            // term.WriteLine($"Lines: {term.Lines}");
            // term.WriteLine($"Columns: {term.Columns}");
            // term.Write($"ansi 8 text: ", "Hello".WithColor(TerminalAnsiColor.Green), "\r\n");
            // term.Write($"ansi 16 text: ", "Hello".WithColor(TerminalAnsiColor.BrightCyan), "\r\n");
            // term.Write($"ansi 231 text: ", "Hello".WithColor(TerminalAnsiColor.Color102), "\r\n");
            // term.Write($"ansi 255 text: ", "Hello".WithColor(TerminalAnsiColor.Color254), "\r\n");
            // _ = TerminalTrueColor.TryParseHex("5865f2", out var col);
            // term.Write($"TrueColor text: ", "Hello".WithColor(col), "\r\n");
            // term.Write($"Attributed text: ", "Hello".WithColor(new TerminalCellColor { Foreground = col, Attributes = TerminalCellColor.CellAttributes.Underline }),
            //         " Bye".WithColor(new TerminalCellColor { Foreground = col, Attributes = TerminalCellColor.CellAttributes.Underline }),
            //         " New".WithColor(new TerminalCellColor { Foreground = TerminalAnsiColor.Blue, Attributes = TerminalCellColor.CellAttributes.Underline }),
            //         "\r\n");
            return 0;
        }
    }
}
