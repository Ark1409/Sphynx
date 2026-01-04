using Sphynx.Client.Tui.Terminal;

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

        static int Main(string[] args)
        {
            var term = WindowsTerminal.StandardTerminal;
            term.WriteLine($"Lines: {term.Lines}");
            term.WriteLine($"Columns: {term.Columns}");
            term.Write($"ansi 8 text:", "Hello".WithColor(TerminalAnsiColor.FromColor(TerminalAnsiColor.AnsiColors.Green)), "\r\n");
            term.Write($"ansi 16 text:", "Hello".WithColor(TerminalAnsiColor.FromColor(TerminalAnsiColor.AnsiColors.BrightCyan)), "\r\n");
            term.Write($"ansi 231 text:", "Hello".WithColor(TerminalAnsiColor.FromColor(TerminalAnsiColor.AnsiColors.Color102)), "\r\n");
            term.Write($"ansi 255 text:", "Hello".WithColor(TerminalAnsiColor.FromColor(TerminalAnsiColor.AnsiColors.Color254)), "\r\n");
            _ = TerminalTrueColor.TryParseHex("5865f2", out var col);
            term.Write($"TrueColor text:", "Hello".WithColor(col), "\r\n");
            term.Write($"Attributed text:", "Hello".WithColor(new TerminalCellColor { Foreground = col, Attributes = TerminalCellColor.CellAttributes.Underline }),
                    "Bye".WithColor(new TerminalCellColor { Foreground = col, Attributes = TerminalCellColor.CellAttributes.Underline }),
                    "New".WithColor(new TerminalCellColor { Foreground = TerminalAnsiColor.FromColor(TerminalAnsiColor.AnsiColors.Blue), Attributes = TerminalCellColor.CellAttributes.Underline }),
                    "\r\n");
            return 0;
        }
    }
}
