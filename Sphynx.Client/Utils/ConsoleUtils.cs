using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Sphynx.Client.Utils
{
    /// <summary>
    /// Class holding utilities for virtual terminal sequences
    /// </summary>
    internal static class ConsoleUtils
    {
        private const Int32 STD_OUTPUT_HANDLE = -11;
        private static readonly IntPtr INVALID_HANDLE_VALUE = new(-1);
        private static IntPtr _hStdOut = INVALID_HANDLE_VALUE;
        private static Int32? _oldConsoleMode = null;

        /// <summary>
        /// Enables parsing of virtual terminal sequences on stdout (console).<br/>
        /// When enabled, characters written into <see cref="Console.Out"/> are parsed for VT100 and similar control character sequences that control cursor movement, 
        /// color/font mode, and other operations that can also be performed via the existing Console APIs.<br/><br/>
        /// 
        /// The default console mode can later be restored using <see cref="DisableVirtualTerminal"/>
        /// </summary>
        /// <returns>True if parsing of virtual terminal sequences was successfully enabled, false otherwise.</returns>
        public static bool EnableVirtualTerminal()
        {
            // Virtual terminal sequences should be (?) enabled by default on most *nix terminal emulators
            // TODO Check if stdout is a tty
            if (OperatingSystem.IsLinux()) return true;

            // Grab handle to stdout if it hasn't been cached
            if (_hStdOut == INVALID_HANDLE_VALUE)
            {
                if ((_hStdOut = GetStdHandle(STD_OUTPUT_HANDLE)) == INVALID_HANDLE_VALUE)
                {
                    return false;
                }
            }

            /// Cache old console mode for restoration later in <see cref="DisableVirtualTerminal"/>
            if (!_oldConsoleMode.HasValue)
            {
                if (GetConsoleMode(_hStdOut, out var consoleMode) == 0)
                {
                    _hStdOut = INVALID_HANDLE_VALUE;
                    return false;
                }
                _oldConsoleMode = consoleMode;
            }

            // Enable virtual terminal sequences on console
            // See https://learn.microsoft.com/en-us/windows/console/setconsolemode

            // Console mode constants
            const Int32 ENABLE_PROCESSED_OUTPUT = 0x0001,
                        ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

            if ((_oldConsoleMode & ENABLE_VIRTUAL_TERMINAL_PROCESSING) == ENABLE_VIRTUAL_TERMINAL_PROCESSING) return true;
            if (SetConsoleMode(_hStdOut, _oldConsoleMode!.Value | ENABLE_PROCESSED_OUTPUT | ENABLE_VIRTUAL_TERMINAL_PROCESSING) != 0) return true;

            _hStdOut = INVALID_HANDLE_VALUE;
            _oldConsoleMode = null;
            return false;
        }

        /// <summary>
        /// Disables parsing of virtual terminal sequences on stdout (console).
        /// This does not actually disable the ability in the console but reset it to its original capabilities.
        /// See <see cref="EnableVirtualTerminal"/> for more information.
        /// </summary>
        public static void DisableVirtualTerminal()
        {
            // No work to do on *nix
            // TODO Check if stdout is a tty
            if (OperatingSystem.IsLinux()) return;

            /// Reset console modes if virtual terminal processing has been enabled prior in <see cref="EnableVirtualTerminal"/>
            if (_hStdOut != INVALID_HANDLE_VALUE && _oldConsoleMode.HasValue)
            {
                _ = SetConsoleMode(_hStdOut, _oldConsoleMode.Value);
            }

            _hStdOut = INVALID_HANDLE_VALUE;
            _oldConsoleMode = null;
        }
        
        /// <summary>
        /// Checks if virtual terminal capabilities has been previously enabled by <see cref="EnableVirtualTerminal"/>.
        /// </summary>
        /// <returns><c>true</c> if virtual terminal capability was previously enabled, <c>false</c> otherwise.</returns>
        public static bool IsVirtualTerminalEnabled()
        {
            // Virtual terminal sequences should be (?) enabled by default on most *nix operating systems
            // TODO Check if stdout is a tty
            if (OperatingSystem.IsLinux()) return true;

            // Grab handle to stdout if it hasn't been cached
            if (_hStdOut == INVALID_HANDLE_VALUE || !_oldConsoleMode.HasValue) return false;

            const Int32 ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
            return (_oldConsoleMode & ENABLE_VIRTUAL_TERMINAL_PROCESSING) == ENABLE_VIRTUAL_TERMINAL_PROCESSING;
        }

        /// <summary>
        /// Switches to a new alternate screen buffer.<br/>
        /// *nix style applications often utilize an alternate screen buffer, so that they can modify the entire contents of the buffer, 
        /// without affecting the application that started them. The alternate buffer is exactly the dimensions of the window, without any scrollback region.
        /// For an example of this behavior, consider when vim is launched from bash. Vim uses the entirety of the screen to edit the file, 
        /// then returning to bash leaves the original buffer unchanged.
        /// </summary>
        public static void SwitchToAlternate() => Console.Write("\x1b[?1049h");

        /// <summary>
        /// Switches to the main buffer.<br/>
        /// *nix style applications often utilize an alternate screen buffer, so that they can modify the entire contents of the buffer, 
        /// without affecting the application that started them. The alternate buffer is exactly the dimensions of the window, 
        /// without any scrollback region.<br/>
        /// For an example of this behavior, consider when vim is launched from bash. Vim uses the entirety of the screen to edit the file, 
        /// then returning to bash leaves the original buffer unchanged.
        /// </summary>
        public static void SwitchToMain() => Console.Write("\x1b[?1049l");

        /// <summary>
        /// The following sequences allow a program to configure the "scrolling region" of the screen that is affected by scrolling operations. 
        /// This is a subset of the rows that are adjusted when the screen would otherwise scroll, for example, on a ‘\n’ or RI. 
        /// These margins also affect the rows modified by Insert Line (IL) and Delete Line (DL), Scroll Up (SU) and Scroll Down (SD).<br/>
        /// The scrolling margins can be especially useful for having a portion of the screen that doesn’t scroll when the rest of the screen is filled, 
        /// such as having a title bar at the top or a status bar at the bottom of your application.<br/>
        /// Scrolling margins are per-buffer, so importantly, the Alternate Buffer and Main Buffer maintain separate scrolling margins settings (so a full screen application in the alternate buffer will not poison the main buffer’s margins).
        /// </summary>
        /// <param name="topRow">Top line of the scroll region, inclusive.</param>
        /// <param name="bottomRow">Bottom line of the scroll region, inclusive.</param>
        public static void SetScrollRegion(int topRow, int bottomRow) => Console.Write($"\x1b[{Math.Clamp(topRow, 1, Console.BufferHeight)};{Math.Clamp(bottomRow, 1, Console.BufferHeight)}r");

        /// <summary>
        /// Some virtual terminal emulators support a palette of colors greater than the 16 colors provided by the Windows Console. 
        /// For these extended colors, the Windows Console will choose the nearest appropriate color from the existing 16 color table for display. 
        /// Unlike typical SGR values above, the extended values will consume additional parameters after the initial indicator according to the table below.
        /// </summary>
        public static void SetForegroundColor(byte r, byte g, byte b) => Console.Write($"\x1b[38;2;{r};{g};{b}m");

        /// <summary>
        /// Some virtual terminal emulators support a palette of colors greater than the 16 colors provided by the Windows Console. 
        /// For these extended colors, the Windows Console will choose the nearest appropriate color from the existing 16 color table for display. 
        /// </summary>
        public static void SetBackgroundColor(byte r, byte g, byte b) => Console.Write($"\x1b[48;2;{r};{g};{b}m");

        public static void ResetColors() => Console.Write("\x1b[0m");
        public static void Bright(bool bright = true) => Console.Write($"\x1b[{(bright ? 1 : 22)}m");

        public static void Underline(bool underline = true) => Console.Write($"\x1b[{(underline ? 4 : 24)}m");

        public static void SetForegroundBlack() => Console.Write("\x1b[30m");
        public static void SetForegroundRed() => Console.Write("\x1b[31m");
        public static void SetForegroundGreen() => Console.Write("\x1b[32m");
        public static void SetForegroundYellow() => Console.Write("\x1b[33m");
        public static void SetForegroundBlue() => Console.Write("\x1b[34m");
        public static void SetForegroundMagenta() => Console.Write("\x1b[35m");
        public static void SetForegroundCyan() => Console.Write("\x1b[36m");
        public static void SetForegroundWhite() => Console.Write("\x1b[37m");
        public static void ResetForegroundColor() => Console.Write("\x1b[39m");
        public static void SetBackgroundBlack() => Console.Write("\x1b[40m");
        public static void SetBackgroundRed() => Console.Write("\x1b[41m");
        public static void SetBackgroundGreen() => Console.Write("\x1b[42m");
        public static void SetBackgroundYellow() => Console.Write("\x1b[43m");
        public static void SetBackgroundBlue() => Console.Write("\x1b[44m");
        public static void SetBackgroundMagenta() => Console.Write("\x1b[45m");
        public static void SetBackgroundCyan() => Console.Write("\x1b[46m");
        public static void SetBackgroundWhite() => Console.Write("\x1b[47m");
        public static void ResetBackgroundColor() => Console.Write("\x1b[49m");

        /// <summary>
        /// Sets the background color of the terminal screen.
        /// </summary>
        /// <param name="r">Red channel</param>
        /// <param name="g">Green channel</param>
        /// <param name="b">Blue channel</param>
        public static void SetScreenColor(byte r, byte g, byte b) => Console.Write($"\x1b]4;0;rgb:{r:X}/{g:X}/{b:X}\x1b\x5c");

        /// <summary>
        /// Swaps foreground and background colors
        /// </summary>
        public static void SwapColors() => Console.Write("\x1b[7");

        /// <summary>
        /// The text is moved starting with the line the cursor is on. If the cursor is on the middle row of the viewport, 
        /// then scroll up would move the bottom half of the viewport, and insert blank lines at the bottom. 
        /// Scroll down would move the top half of the viewport’s rows, and insert new lines at the top.
        /// </summary>
        /// <param name="count">The amount of rows the viewport should be scrolled up by.</param>
        public static void ScrollUp(int count)
        {
            if (count < 0) ScrollDown(-count);
            // Flipped since doc talks about text movement, not viewport
            else Console.Write($"\x1b[{count}T");
        }

        /// <summary>
        /// The text is moved starting with the line the cursor is on. If the cursor is on the middle row of the viewport, 
        /// then scroll up would move the bottom half of the viewport, and insert blank lines at the bottom. 
        /// Scroll down would move the top half of the viewport’s rows, and insert new lines at the top.
        /// </summary>
        /// <param name="count">The amount of rows the viewport should be scrolled down by.</param>
        public static void ScrollDown(int count)
        {
            if (count < 0) ScrollUp(-count);
            // Flipped since doc talks about text movement, not viewport
            else Console.Write($"\x1b[{count}S");
        }

        /// <summary>
        /// The following command controls and allows for customization of the cursor shape.
        /// </summary>
        /// <param name="type">The cursor type to use.</param>
        public static void SetCursorType(CursorType type) => Console.Write($"\x1b[{(int)type}\x20q");

        public enum CursorType
        {
            /// <summary>
            /// Default cursor shape configured by the user
            /// </summary>
            DEFAULT = 0,

            /// <summary>
            /// Blinking block cursor shape
            /// </summary>
            BLINKING_BLOCK = 1,

            /// <summary>
            /// Steady block cursor shape
            /// </summary>
            STEADY_BLOCK = 2,

            /// <summary>
            /// Blinking underline cursor shape
            /// </summary>
            BLINKING_UNDERLINE = 3,

            /// <summary>
            /// Steady underline cursor shape
            /// </summary>
            STEADY_UNDERLINE = 4,

            /// <summary>
            /// Blinking bar cursor shape
            /// </summary>
            BLINKING_BAR = 5,

            /// <summary>
            /// Steady bar cursor shape
            /// </summary>
            STEADY_BAR = 6
        }

        /// <summary>
        /// The following commands control the visibility of the cursor and its blinking state. 
        /// The DECTCEM sequences are generally equivalent to calling SetConsoleCursorInfo console API to toggle cursor visibility.<br/>
        /// 
        /// Hides the console cursor.
        /// </summary>
        public static void HideCursor() => Console.Write("\x1b[?25l");

        /// <summary>
        /// The following commands control the visibility of the cursor and its blinking state. 
        /// The DECTCEM sequences are generally equivalent to calling SetConsoleCursorInfo console API to toggle cursor visibility.<br/>
        /// 
        /// Enables showing of the console cursor
        /// </summary>
        public static void ShowCursor() => Console.Write("\x1b[?25h");

        /// <summary>
        /// Replace all text in the current viewport/screen with space characters.
        /// </summary>
        public static void Clear() => Console.Write("\x1b[1;1H\x1b[2J");

        /// <summary>
        /// Replace all text on the line with the cursor with space characters.
        /// </summary>
        public static void ClearLine() => Console.Write("\x1b[2K");

        /// <summary>
        /// Sets the console window’s title to the specified string.
        /// </summary>
        /// <param name="title">The new title for the window.</param>
        public static void SetTitle(string title) => Console.Write($"\x1b]0;{title}\x1b\x5c");

        /// <summary>
        /// Cursor moves to &lt;x&gt;; &lt;y&gt; coordinate within the viewport, where &lt;x&gt; is the column of the &lt;y&gt; line
        /// </summary>
        /// <param name="x">The column number</param>
        /// <param name="y">The row number</param>
        public static void MoveTo(int x, int y) => MoveTo(new Point(x, y));

        /// <summary>
        /// Cursor moves to &lt;x&gt;; &lt;y&gt; coordinate within the viewport, where &lt;x&gt; is the column of the &lt;y&gt; line
        /// </summary>
        /// <param name="pos">The position to which the cursor should move</param>
        public static void MoveTo(Point pos) => Console.Write($"\x1b[{Math.Clamp(pos.Y, 1, Console.BufferHeight)};{Math.Clamp(pos.X, 1, Console.BufferWidth)}H");

        [SupportedOSPlatform("windows")]
        public static bool SetConsoleWindowPosition(int x, int y, bool absolute = true) => SetConsoleWindowPosition(new Point(x, y), absolute);

        [SupportedOSPlatform("windows")]
        public static bool SetConsoleWindowPosition(Point pos, bool absolute = true)
        {
            if (_hStdOut == INVALID_HANDLE_VALUE)
            {
                if ((_hStdOut = GetStdHandle(STD_OUTPUT_HANDLE)) == INVALID_HANDLE_VALUE)
                {
                    return false;
                }
            }

            CONSOLE_SCREEN_BUFFER_INFO info = default;
            if (GetConsoleScreenBufferInfo(_hStdOut, out info) == 0x0) return false;

            SMALL_RECT rect = default;
            rect.Left = (short)(absolute ? pos.X : rect.Left + pos.X);
            rect.Top = (short)(absolute ? pos.Y : rect.Top + pos.Y);
            rect.Right = (short)(rect.Left + info.srWindow.Right - info.srWindow.Left);
            rect.Bottom = (short)(rect.Top + info.srWindow.Bottom - info.srWindow.Top);
            return SetConsoleWindowInfo(_hStdOut, 1, rect) != 0;
        }

        [SupportedOSPlatform("windows")]
        public static bool SetConsoleWindowSize(int width, int height)
        {
            if (_hStdOut == INVALID_HANDLE_VALUE)
            {
                if ((_hStdOut = GetStdHandle(STD_OUTPUT_HANDLE)) == INVALID_HANDLE_VALUE)
                {
                    return false;
                }
            }

            if (GetConsoleScreenBufferInfo(_hStdOut, out CONSOLE_SCREEN_BUFFER_INFO info) == 0x0) return false;

            SMALL_RECT rect = default;
            rect.Left = info.srWindow.Left;
            rect.Top = info.srWindow.Top;
            rect.Right = (short)(rect.Left + width);
            rect.Bottom = (short)(rect.Top + height);
            return SetConsoleWindowInfo(_hStdOut, 1, rect) != 0;
        }

        [DllImport("api-ms-win-core-console-l1-1-0.dll", SetLastError = true)]
        private static extern Int32 GetConsoleMode(IntPtr hConsoleHandle, out Int32 lpMode);

        [DllImport("api-ms-win-core-console-l1-1-0.dll", SetLastError = true)]
        private static extern Int32 SetConsoleMode(IntPtr hConsoleHandle, Int32 dwMode);

        [DllImport("api-ms-win-core-processenvironment-l1-1-0.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(Int32 nStdHandle);

        [StructLayout(LayoutKind.Sequential)]
        private struct SMALL_RECT
        {
            public short Left;
            public short Top;
            public short Right;
            public short Bottom;
        }

        [DllImport("api-ms-win-core-console-l2-1-0.dll", SetLastError = true)]
        private static extern int SetConsoleWindowInfo(IntPtr hConsoleOutput, int bAbsolute, in SMALL_RECT lpConsoleWindow);

        [StructLayout(LayoutKind.Sequential)]
        private struct COORD
        {
            public short X;
            public short Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CONSOLE_SCREEN_BUFFER_INFO
        {
            public COORD dwSize;
            public COORD dwCursorPosition;
            public Int16 wAttributes;
            public SMALL_RECT srWindow;
            public COORD dwMaximumWindowSize;
        }

        [DllImport("api-ms-win-core-console-l2-1-0.dll", SetLastError = true)]
        private static extern int GetConsoleScreenBufferInfo(IntPtr hConsoleOutput, out CONSOLE_SCREEN_BUFFER_INFO lpConsoleScreenBufferInfo);
    }
}
