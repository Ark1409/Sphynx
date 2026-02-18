// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using UINT = uint;
using ULONG = uint;
using USHORT = ushort;
using SHORT = short;
using WORD = ushort;
using DWORD = uint;
using HANDLE = System.IntPtr;
using BOOL = int;
using COLORREF = int;
using WCHAR = char;
using CHAR = byte;

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.ComponentModel;

namespace Sphynx.Client.Tui
{
    internal static class WindowsTerminalInterop
    {
        public const DWORD STD_INPUT_HANDLE = unchecked((DWORD)(-10));
        public const DWORD STD_OUTPUT_HANDLE = unchecked((DWORD)(-11));
        public const DWORD STD_ERROR_HANDLE = unchecked((DWORD)(-12));

        private static HANDLE? _stdIn;
        private static HANDLE? _stdOut;
        private static HANDLE? _stdErr;

        public static HANDLE StdIn => _stdIn ??= GetStdHandle(STD_INPUT_HANDLE);
        public static HANDLE StdOut => _stdOut ??= GetStdHandle(STD_OUTPUT_HANDLE);
        public static HANDLE StdErr => _stdErr ??= GetStdHandle(STD_ERROR_HANDLE);

        public const DWORD ENABLE_PROCESSED_OUTPUT = 0x0001;
        public const DWORD ENABLE_WRAP_AT_EOL_OUTPUT = 0x0002;
        public const DWORD ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
        public const DWORD DISABLE_NEWLINE_AUTO_RETURN = 0x0008;

        public const DWORD ENABLE_PROCESSED_INPUT = 0x0001;
        public const DWORD ENABLE_LINE_INPUT = 0x0002;
        public const DWORD ENABLE_ECHO_INPUT = 0x0004;
        public const DWORD ENABLE_WINDOW_INPUT = 0x0008;
        public const DWORD ENABLE_MOUSE_INPUT = 0x0010;
        public const DWORD ENABLE_INSERT_MODE = 0x0020;
        public const DWORD ENABLE_QUICK_EDIT_MODE = 0x0040;
        public const DWORD ENABLE_EXTENDED_FLAGS = 0x0080;
        public const DWORD ENABLE_VIRTUAL_TERMINAL_INPUT = 0x0200;

        public const DWORD KEY_EVENT = 0x0001;
        public const DWORD MOUSE_EVENT = 0x0002;
        public const DWORD WINDOW_BUFFER_SIZE_EVENT = 0x0004;
        public const DWORD MENU_EVENT = 0x0008;
        public const DWORD FOCUS_EVENT = 0x0010;

        [DllImport("api-ms-win-core-console-l1-1-0.dll", SetLastError = true)]
        public static extern BOOL GetConsoleMode(HANDLE hConsoleHandle, out DWORD lpMode);

        [DllImport("api-ms-win-core-console-l1-1-0.dll", SetLastError = true)]
        public static extern BOOL SetConsoleMode(HANDLE hConsoleHandle, DWORD dwMode);

        [DllImport("api-ms-win-core-processenvironment-l1-1-0.dll", SetLastError = true)]
        public static extern HANDLE GetStdHandle(DWORD nStdHandle);

        public static bool AddConsoleModes(HANDLE hConsoleHandle, DWORD modes)
        {
            BOOL ret = 0;
            ret = GetConsoleMode(hConsoleHandle, out var currentMode);
            if (ret == 0) return false;

            if ((currentMode & modes) == modes) return true;
            currentMode |= modes;
            ret = SetConsoleMode(hConsoleHandle, currentMode);
            if (ret == 0) return false;

            return true;
        }

        public static bool RemoveConsoleModes(HANDLE hConsoleHandle, DWORD modes)
        {
            BOOL ret = 0;
            ret = GetConsoleMode(hConsoleHandle, out var currentMode);
            if (ret == 0) return false;

            if ((currentMode & ~modes) == 0x0) return true;
            currentMode &= ~modes;
            ret = SetConsoleMode(hConsoleHandle, currentMode);
            if (ret == 0) return false;

            return true;
        }

        public static bool AddAnsiEscapeOutput(HANDLE h) => AddConsoleModes(h, ENABLE_PROCESSED_OUTPUT | ENABLE_VIRTUAL_TERMINAL_PROCESSING);
        public static bool AddAnsiEscapeInput(HANDLE h) => AddConsoleModes(h, ENABLE_VIRTUAL_TERMINAL_INPUT);

        public static bool AddMouseInput(HANDLE hConsoleHandle)
        {
            BOOL ret = 0;
            ret = GetConsoleMode(hConsoleHandle, out var currentMode);
            if (ret == 0) return false;

            DWORD newMode = currentMode;
            newMode = (newMode | ENABLE_MOUSE_INPUT | ENABLE_EXTENDED_FLAGS) & ~ENABLE_QUICK_EDIT_MODE;
            if (newMode == currentMode) return true;
            ret = SetConsoleMode(hConsoleHandle, newMode);
            if (ret == 0) return false;

            return true;
        }

        public static bool AddWindowInput(HANDLE h) => AddConsoleModes(h, ENABLE_WINDOW_INPUT);

        public static bool AddAnsiEscapeOutput() => AddAnsiEscapeOutput(StdOut);
        public static bool AddAnsiEscapeInput() => AddAnsiEscapeInput(StdIn);
        public static bool AddMouseInput() => AddMouseInput(StdIn);
        public static bool AddWindowInput() => AddWindowInput(StdIn);

        public static bool SetupConsoleBase(HANDLE hInputHandle, HANDLE hOutputHandle)
        {
            {
                BOOL ret = 0;
                ret = GetConsoleMode(hInputHandle, out var currentMode);
                if (ret == 0) return false;

                DWORD newMode = currentMode;

                newMode |= newMode | ENABLE_EXTENDED_FLAGS | ENABLE_INSERT_MODE;
                newMode &= ~(ENABLE_ECHO_INPUT | ENABLE_PROCESSED_INPUT | ENABLE_LINE_INPUT);

                if (newMode != currentMode)
                {
                    ret = SetConsoleMode(hInputHandle, newMode);
                    if (ret == 0)
                    {
                        _ = SetConsoleMode(hInputHandle, currentMode);
                        return false;
                    }
                }
            }

            {
                BOOL ret = 0;
                ret = GetConsoleMode(hOutputHandle, out var currentMode);
                if (ret == 0) return false;

                DWORD newMode = currentMode;

                newMode |= newMode | ENABLE_WRAP_AT_EOL_OUTPUT | DISABLE_NEWLINE_AUTO_RETURN;
                newMode &= ~(ENABLE_ECHO_INPUT | ENABLE_PROCESSED_INPUT | ENABLE_LINE_INPUT);

                if (newMode != currentMode)
                {
                    ret = SetConsoleMode(hOutputHandle, newMode);
                    if (ret == 0)
                    {
                        _ = SetConsoleMode(hOutputHandle, currentMode);
                        return false;
                    }
                }

            }
            return true;
        }

        [DllImport("api-ms-win-core-file-l1-1-0.dll", SetLastError = true)]
        public static extern BOOL WriteFile(HANDLE hFile, IntPtr lpBuffer, DWORD nNumberOfBytesToWrite, out DWORD lpNumberOfBytesWritten, IntPtr lpOverlapped);

        [DllImport("api-ms-win-core-file-l1-1-0.dll", SetLastError = true)]
        public static extern DWORD SetFilePointer(HANDLE hFile, int lDistanceToMove, ref int lpDistanceToMoveHigh, DWORD dwMoveMethod);

        public const DWORD FILE_BEGIN = 0;
        public const DWORD FILE_CURRENT = 1;
        public const DWORD FILE_END = 2;

        public const DWORD INVALID_SET_FILE_POINTER = unchecked((DWORD)(-1));

        [DllImport("api-ms-win-core-file-l1-1-0.dll", SetLastError = true)]
        public static extern BOOL FlushFileBuffers(HANDLE hFile);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckWin32Return(BOOL ret)
        {
            if (ret == 0)
                ThrowLastWin32Error();
        }

        public const DWORD ERROR_SUCCESS = 0;

        public static void ThrowLastWin32Error()
        {
            var lastError = Marshal.GetLastWin32Error();
            if (lastError != ERROR_SUCCESS)
                throw new Win32Exception(lastError);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct COORD
        {
            public SHORT X;
            public SHORT Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SMALL_RECT
        {
            public SHORT Left;
            public SHORT Top;
            public SHORT Right;
            public SHORT Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CONSOLE_SCREEN_BUFFER_INFO
        {
            public COORD dwSize;
            public COORD dwCursorPosition;
            public WORD wAttributes;
            public SMALL_RECT srWindow;
            public COORD dwMaximumWindowSize;
        }

        [DllImport("api-ms-win-core-console-l2-1-0.dll", SetLastError = true)]
        public static extern BOOL GetConsoleScreenBufferInfo(HANDLE hConsoleOutput, out CONSOLE_SCREEN_BUFFER_INFO lpConsoleScreenBufferInfo);

        [StructLayout(LayoutKind.Sequential)]
        public struct CONSOLE_SCREEN_BUFFER_INFOEX
        {
            public ULONG cbSize;
            public COORD dwSize;
            public COORD dwCursorPosition;
            public WORD wAttributes;
            public SMALL_RECT srWindow;
            public COORD dwMaximumWindowSize;
            public WORD wPopupAttributes;
            public BOOL bFullscreenSupported;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16, ArraySubType = UnmanagedType.I4)]
            public COLORREF[] ColorTable;
        }

        [DllImport("api-ms-win-core-console-l2-1-0.dll", SetLastError = true)]
        public static extern BOOL GetConsoleScreenBufferInfoEx(HANDLE hConsoleOutput, out CONSOLE_SCREEN_BUFFER_INFOEX lpConsoleScreenBufferInfoEx);

        public static (byte R, byte G, byte B) GetRGB(COLORREF col)
        {
            // TODO: Check whether windows reverses order in 32-bit? (I don't think so...)
            return ((byte)(col & 0xff), (byte)((col >> 8) & 0xff), (byte)((col >> 16) & 0xff));
        }

        public enum InputEventType : WORD
        {
            KEY_EVENT = 0x0001,
            MOUSE_EVENT = 0x0002,
            WINDOW_BUFFER_SIZE_EVENT = 0x0004,
            MENU_EVENT = 0x0008,
            FOCUS_EVENT = 0x0010
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT_RECORD
        {
            public InputEventType EventType;
            public EventUnion Event;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct EventUnion
        {
            [FieldOffset(0)]
            public KEY_EVENT_RECORD KeyEvent;

            [FieldOffset(0)]
            public MOUSE_EVENT_RECORD MouseEvent;

            [FieldOffset(0)]
            public WINDOW_BUFFER_SIZE_RECORD WindowBufferSizeEvent;

            [FieldOffset(0)]
            public MENU_EVENT_RECORD MenuEvent;

            [FieldOffset(0)]
            public FOCUS_EVENT_RECORD FocusEvent;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KEY_EVENT_RECORD
        {
            [MarshalAs(UnmanagedType.Bool)]
            public BOOL bKeyDown;

            public WORD wRepeatCount;
            public WORD wVirtualKeyCode;
            public WORD wVirtualScanCode;

            public KeyEventChar uChar;

            public DWORD dwControlKeyState;

            [StructLayout(LayoutKind.Explicit)]
            public struct KeyEventChar
            {
                [FieldOffset(0)]
                public WCHAR UnicodeChar;

                [FieldOffset(0)]
                public CHAR AsciiChar;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSE_EVENT_RECORD
        {
            public COORD dwMousePosition;
            public uint dwButtonState;
            public uint dwControlKeyState;
            public uint dwEventFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOW_BUFFER_SIZE_RECORD
        {
            public COORD dwSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MENU_EVENT_RECORD
        {
            public UINT dwCommandId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct FOCUS_EVENT_RECORD
        {
            [MarshalAs(UnmanagedType.Bool)]
            public BOOL bSetFocus;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        public static extern BOOL ReadConsoleInputExW(
                HANDLE hConsoleInput,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] INPUT_RECORD[] lpBuffer,
                DWORD nLength,
                out DWORD lpNumberOfEventsRead, USHORT wFlags);

        [DllImport("api-ms-win-core-console-l1-2-0.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        public static extern BOOL PeekConsoleInputW(
                HANDLE hConsoleInput,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] INPUT_RECORD[] lpBuffer,
                DWORD nLength,
                out DWORD lpNumberOfEventsRead);
    }
}
