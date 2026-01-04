// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.


using System.Runtime.InteropServices;
using Nerdbank.Streams;

using DWORD = int;
using BOOL = int;

using HANDLE = System.IntPtr;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Win32.SafeHandles;
using System.Text;

namespace Sphynx.Client.Tui.Terminal
{
    public class HandleWindowsTerminal : WindowsTerminal
    {
        protected SafeFileHandle Input { get; }
        protected SafeFileHandle Output { get; }

        private bool? _hasTrueColorCache;
        public override bool HasTrueColor => _hasTrueColorCache ??= WindowsTerminalInterop.AddAnsiEscapeOutput(Output.DangerousGetHandle());

        private bool? _hasMouseSupportCache;
        public override bool HasMouseSupport => _hasMouseSupportCache ??= WindowsTerminalInterop.AddMouseInput(Input.DangerousGetHandle());

        public override int Lines => throw new NotImplementedException();

        public override int Columns => throw new NotImplementedException();

        public HandleWindowsTerminal(SafeFileHandle input, SafeFileHandle output)
            : base(FullDuplexStream.Splice(readableStream: new FileStream(input, FileAccess.Read), writableStream: new FileStream(output, FileAccess.Write)))
        {
            Input = input;
            Output = output;
        }

        protected HandleWindowsTerminal(SafeFileHandle input, SafeFileHandle output, Stream s) : base(s)
        {
            Input = input;
            Output = output;
        }
    }

    internal class StandardWindowsTerminal : HandleWindowsTerminal
    {
        private static bool? _hasTrueColorCache;

        // TODO: Also check if windows version is greater than
        public override bool HasTrueColor => _hasTrueColorCache ??= WindowsTerminalInterop.AddAnsiEscapeOutput();

        private static bool? _hasMouseSupportCache;
        public override bool HasMouseSupport => _hasMouseSupportCache ??= WindowsTerminalInterop.AddMouseInput();

        public override int Lines => Console.WindowHeight;
        public override int Columns => Console.WindowWidth;

        internal StandardWindowsTerminal()
            : base(new SafeFileHandle(WindowsTerminalInterop.StdIn, false), new SafeFileHandle(WindowsTerminalInterop.StdOut, false),
                    FullDuplexStream.Splice(Console.OpenStandardInput(), Console.OpenStandardOutput()))
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;
        }
    }

    internal static class WindowsTerminalInterop
    {
        private static HANDLE? _stdIn;
        private static HANDLE? _stdOut;
        private static HANDLE? _stdErr;

        public static HANDLE StdIn => _stdIn ??= GetStdHandle(STD_INPUT_HANDLE);
        public static HANDLE StdOut => _stdOut ??= GetStdHandle(STD_OUTPUT_HANDLE);
        public static HANDLE StdErr => _stdErr ??= GetStdHandle(STD_ERROR_HANDLE);

        public const DWORD STD_INPUT_HANDLE = -10;
        public const DWORD STD_OUTPUT_HANDLE = -11;
        public const DWORD STD_ERROR_HANDLE = -12;

        public const DWORD ENABLE_PROCESSED_OUTPUT = 0x0001;
        public const DWORD ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

        public const DWORD ENABLE_VIRTUAL_TERMINAL_INPUT = 0x0200;
        public const DWORD ENABLE_MOUSE_INPUT = 0x0010;
        public const DWORD ENABLE_WINDOW_INPUT =  0x0008;

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

        public static bool AddAnsiEscapeOutput(HANDLE h) => AddConsoleModes(h, ENABLE_PROCESSED_OUTPUT | ENABLE_VIRTUAL_TERMINAL_PROCESSING);
        public static bool AddAnsiEscapeInput(HANDLE h) => AddConsoleModes(h, ENABLE_VIRTUAL_TERMINAL_INPUT);
        public static bool AddMouseInput(HANDLE h) => AddConsoleModes(h, ENABLE_MOUSE_INPUT);
        public static bool AddWindowInput(HANDLE h) => AddConsoleModes(h, ENABLE_WINDOW_INPUT);

        public static bool AddAnsiEscapeOutput() => AddAnsiEscapeOutput(StdOut);
        public static bool AddAnsiEscapeInput() => AddAnsiEscapeInput(StdIn);
        public static bool AddMouseInput() => AddMouseInput(StdIn);
        public static bool AddWindowInput() => AddMouseInput(StdIn);

        [DllImport("api-ms-win-core-file-l1-1-0.dll", SetLastError = true)]
        public static extern BOOL WriteFile(HANDLE hFile, IntPtr lpBuffer, DWORD nNumberOfBytesToWrite, out DWORD lpNumberOfBytesWritten, IntPtr lpOverlapped);

        [DllImport("api-ms-win-core-file-l1-1-0.dll", SetLastError = true)]
        public static extern DWORD SetFilePointer(HANDLE hFile, int lDistanceToMove, ref int lpDistanceToMoveHigh, DWORD dwMoveMethod);

        public const DWORD FILE_BEGIN = 0;
        public const DWORD FILE_CURRENT = 1;
        public const DWORD FILE_END = 2;

        public const DWORD INVALID_SET_FILE_POINTER = (DWORD)(-1);

        [DllImport("api-ms-win-core-file-l1-1-0.dll", SetLastError = true)]
        public static extern BOOL FlushFileBuffers(HANDLE hFile);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckWin32Return(BOOL ret)
        {
            if (ret != 0)
                ThrowLastWin32Error();
        }

        public const DWORD ERROR_SUCCESS = 0;

        public static void ThrowLastWin32Error()
        {
            var lastError = Marshal.GetLastWin32Error();
            if (lastError != ERROR_SUCCESS)
                throw new Win32Exception(lastError);
        }
    }
}
