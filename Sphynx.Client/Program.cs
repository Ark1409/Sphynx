// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Client.Tui;

namespace Sphynx.Client
{
    internal static class Program
    {
        private static async Task<int> Run()
        {
            await using Terminal term = NCursesTerminal.Default;
            term.Init();

            var poller = new TerminalEventPoller(term);

            bool shouldRun = true;
            term.WriteLine("Gonna poll event");
            term.Flush();

            var col = TerminalAnsiColor.BrightRed;
            int i = 0;
            while (shouldRun)
            {
                i = (i + 1) % 256;
                col = TerminalAnsiColor.FromColor((TerminalAnsiColor.AnsiColors)i);
                var ev = poller.PollEvent();

                if (ev.WindowEvent is not null)
                {
                    var wv = ev.WindowEvent.Value;
                    term.WriteLine($"Console was resized to {wv.NewSize}");
                }
                else if (ev.KeyEvent is not null)
                {
                    var kv = ev.KeyEvent.Value;
                    var val = kv.Key.UnicodeChar?.Value;
                    var str = kv.Key.KeyString;
                    if (val is < 0x20 or < 256 and > 0x7e)
                    {
                        str = "UNPRINT";
                    }
                    else if (str is null)
                    {
                        str = $"{kv.Key.Key!}";
                        val = (int)kv.Key.Key!;
                    }

                    if (kv.Key.Key == TerminalKey.SpecialKey.Left)
                    {
                        term.MoveCursorLeft(2);
                    }
                    else if (kv.Key.Key == TerminalKey.SpecialKey.Right)
                    {
                        term.MoveCursorRight(2);
                    }
                    else if (kv.Key.Key == TerminalKey.SpecialKey.Up)
                    {
                        term.MoveCursorUp();
                    }
                    else if (kv.Key.Key == TerminalKey.SpecialKey.Down)
                    {
                        term.MoveCursorDown();
                    }
                    else if (kv.Key.AsciiChar == 0x7f)
                    {
                        term.Erase();
                    }
                    else if (kv.Key.Key == TerminalKey.SpecialKey.Delete)
                    {
                        term.Erase(-1);
                    }
                    else if (kv.Key.KeyString == "z")
                    {
                        term.Write(term.CursorPosition.ToString());
                    }
                    else
                    {
                        term.Write(str.WithColor(ITerminalColor.DefaultColor, col));
                    }


                    if (kv.Key.AsciiChar == 'c' && kv.Key.HasModifiers(TerminalKeyModifiers.Control))
                    {
                        term.WriteLine($"Exiting...");
                        shouldRun = false;
                    }
                }
                else if (ev.MouseClickEvent is { } clickEv)
                {
                    term.WriteLine($"{clickEv.Position} {clickEv.Buttons} V: {clickEv.ClickCount} (Mods: {clickEv.Mods})");
                }
                else if (ev.MouseMoveEvent is { } moveEv)
                {
                    term.WriteLine($"{moveEv.OldPos} -> {moveEv.Position}");
                }
                else if (ev.MouseScrollEvent is { } scrollEv)
                {
                    term.WriteLine($"Scroll: {scrollEv.Position} {scrollEv.Direction} {scrollEv.Delta} (Mods: {scrollEv.Mods})");
                }

                term.Flush();
            }
            return 0;
        }

        private static int Main(string[] args)
        {
            var t = Run();
            t.Wait();
            return t.Result;
        }
    }
}
