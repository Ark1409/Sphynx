// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;

namespace Sphynx.Client.Tui
{
    public static unsafe partial class LibcInterop
    {
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct Termios
        {
            public InputFlags c_iflag; // input mode flags
            public OutputFlags c_oflag; // output mode flags
            public ControlFlags c_cflag; // control mode flags
            public LocalFlags c_lflag; // local mode flags
            public byte c_line; // line discipline
            public fixed byte c_cc[32]; // control characters  (NCCS = 32)
            public uint c_ispeed; // input baud rate
            public uint c_ospeed; // output baud rate
        }

        public static class Cc
        {
            public const int VINTR = 0;
            public const int VQUIT = 1;
            public const int VERASE = 2;
            public const int VKILL = 3;
            public const int VEOF = 4;
            public const int VTIME = 5;
            public const int VMIN = 6;
            public const int VSWTC = 7;
            public const int VSTART = 8;
            public const int VSTOP = 9;
            public const int VSUSP = 10;
            public const int VEOL = 11;
            public const int VREPRINT = 12;
            public const int VDISCARD = 13;
            public const int VWERASE = 14;
            public const int VLNEXT = 15;
            public const int VEOL2 = 16;
            public const int NCCS = 32;
        }

        [Flags]
        public enum InputFlags : uint
        {
            None = 0,
            IGNBRK = 0b_0000_0000_0000_0001, // 0000001 - ignore break condition
            BRKINT = 0b_0000_0000_0000_0010, // 0000002 - signal interrupt on break
            IGNPAR = 0b_0000_0000_0000_0100, // 0000004 - ignore parity errors
            PARMRK = 0b_0000_0000_0000_1000, // 0000010 - mark parity/framing errors
            INPCK = 0b_0000_0000_0001_0000, // 0000020 - enable input parity check
            ISTRIP = 0b_0000_0000_0010_0000, // 0000040 - strip 8th bit
            INLCR = 0b_0000_0000_0100_0000, // 0000100 - map NL to CR on input
            IGNCR = 0b_0000_0000_1000_0000, // 0000200 - ignore CR
            ICRNL = 0b_0000_0001_0000_0000, // 0000400 - map CR to NL on input
            IUCLC = 0b_0000_0010_0000_0000, // 0001000 - map uppercase to lowercase (non-POSIX)
            IXON = 0b_0000_0100_0000_0000, // 0002000 - enable start/stop output control
            IXANY = 0b_0000_1000_0000_0000, // 0004000 - any char restarts output
            IXOFF = 0b_0001_0000_0000_0000, // 0010000 - enable start/stop input control
            IMAXBEL = 0b_0010_0000_0000_0000, // 0020000 - ring bell when input queue full (non-POSIX)
            IUTF8 = 0b_0100_0000_0000_0000, // 0040000 - input is UTF-8 (non-POSIX)
        }

        // ── c_oflag ───────────────────────────────────────────────────────────────────

        [Flags]
        public enum OutputFlags : uint
        {
            None = 0,
            OPOST = 0x0001, // 0000001 - post-process output
            OLCUC = 0x0002, // 0000002 - map lowercase to uppercase on output (non-POSIX)
            ONLCR = 0x0004, // 0000004 - map NL to CR-NL on output
            OCRNL = 0x0008, // 0000010 - map CR to NL on output
            ONOCR = 0x0010, // 0000020 - no CR at column 0
            ONLRET = 0x0020, // 0000040 - NL performs CR function
            OFILL = 0x0040, // 0000100 - use fill characters for delay
            OFDEL = 0x0080, // 0000200 - fill is DEL

            // Newline delay
            NLDLY = 0x0100, // 0000400
            NL0 = 0x0000,
            NL1 = 0x0100,

            // Carriage-return delay
            CRDLY = 0x0600, // 0003000
            CR0 = 0x0000,
            CR1 = 0x0200, // 0001000
            CR2 = 0x0400, // 0002000
            CR3 = 0x0600, // 0003000

            // Horizontal-tab delay
            TABDLY = 0x1800, // 0014000
            TAB0 = 0x0000,
            TAB1 = 0x0800, // 0004000
            TAB2 = 0x1000, // 0010000
            TAB3 = 0x1800, // 0014000 - expand tabs to spaces
            XTABS = 0x1800, // alias

            // Backspace delay
            BSDLY = 0x2000, // 0020000
            BS0 = 0x0000,
            BS1 = 0x2000,

            // Vertical-tab delay
            VTDLY = 0x4000, // 0040000
            VT0 = 0x0000,
            VT1 = 0x4000,

            // Form-feed delay
            FFDLY = 0x8000, // 0100000
            FF0 = 0x0000,
            FF1 = 0x8000,
        }

        [Flags]
        public enum ControlFlags : uint
        {
            None = 0,

            // Character size mask & values
            CSIZE = 0x0030, // 0000060
            CS5 = 0x0000, // 0000000
            CS6 = 0x0010, // 0000020
            CS7 = 0x0020, // 0000040
            CS8 = 0x0030, // 0000060

            CSTOPB = 0x0040, // 0000100 - two stop bits
            CREAD = 0x0080, // 0000200 - enable receiver
            PARENB = 0x0100, // 0000400 - parity enable
            PARODD = 0x0200, // 0001000 - odd parity
            HUPCL = 0x0400, // 0002000 - hang up on last close
            CLOCAL = 0x0800, // 0004000 - ignore modem status lines

            ADDRB = 0x20000000u, // 04000000000 - address bit (non-POSIX)
            CMSPAR = 0x40000000u, // 010000000000 - mark/space parity (non-POSIX)
            CRTSCTS = 0x80000000u, // 020000000000 - hardware flow control (non-POSIX)
        }

        [Flags]
        public enum LocalFlags : uint
        {
            None = 0,
            ISIG = 0x0001, // 0000001 - enable signals
            ICANON = 0x0002, // 0000002 - canonical input
            XCASE = 0x0004, // 0000004 - (non-POSIX)
            ECHO = 0x0008, // 0000010 - enable echo
            ECHOE = 0x0010, // 0000020 - echo erase as backspace
            ECHOK = 0x0020, // 0000040 - echo KILL
            ECHONL = 0x0040, // 0000100 - echo NL
            NOFLSH = 0x0080, // 0000200 - disable flush after interrupt/quit
            TOSTOP = 0x0100, // 0000400 - SIGTTOU for background output
            ECHOCTL = 0x0200, // 0001000 - echo control chars as ^X (non-POSIX)
            ECHOPRT = 0x0400, // 0002000 - echo chars while erasing (non-POSIX)
            ECHOKE = 0x0800, // 0004000 - echo KILL by erasing line (non-POSIX)
            FLUSHO = 0x1000, // 0010000 - output being flushed (non-POSIX)
            PENDIN = 0x4000, // 0040000 - retype pending input (non-POSIX)
            IEXTEN = 0x8000, // 0100000 - enable implementation-defined processing
            EXTPROC = 0x10000, // 0200000 - (non-POSIX)
        }

        public enum TcSetAttrAction : int
        {
            TCSANOW = 0, // change immediately
            TCSADRAIN = 1, // change after all output is transmitted
            TCSAFLUSH = 2, // change after flush of pending input
        }

        // ── tcflush queue_selector ────────────────────────────────────────────────────

        public enum TcFlushQueue : int
        {
            TCIFLUSH = 0, // flush received but not read
            TCOFLUSH = 1, // flush written but not transmitted
            TCIOFLUSH = 2, // flush both
        }

        public enum TcFlowAction : int
        {
            TCOOFF = 0, // suspend output
            TCOON = 1, // restart output
            TCIOFF = 2, // transmit STOP character
            TCION = 3, // transmit START character
        }

        public static class BaudRate
        {
            // POSIX required
            public const uint B0 = 0u;
            public const uint B50 = 50u;
            public const uint B75 = 75u;
            public const uint B110 = 110u;
            public const uint B134 = 134u; // Really 134.5 by POSIX spec
            public const uint B150 = 150u;
            public const uint B200 = 200u;
            public const uint B300 = 300u;
            public const uint B600 = 600u;
            public const uint B1200 = 1200u;
            public const uint B1800 = 1800u;
            public const uint B2400 = 2400u;
            public const uint B4800 = 4800u;
            public const uint B9600 = 9600u;
            public const uint B19200 = 19200u;
            public const uint B38400 = 38400u;
            public const uint EXTA = B19200;
            public const uint EXTB = B38400;

            // Non-standard but common
            public const uint B7200 = 7200u;
            public const uint B14400 = 14400u;
            public const uint B28800 = 28800u;
            public const uint B33600 = 33600u;
            public const uint B57600 = 57600u;
            public const uint B76800 = 76800u;
            public const uint B115200 = 115200u;
            public const uint B153600 = 153600u;
            public const uint B230400 = 230400u;
            public const uint B307200 = 307200u;
            public const uint B460800 = 460800u;
            public const uint B500000 = 500000u;
            public const uint B576000 = 576000u;
            public const uint B614400 = 614400u;
            public const uint B921600 = 921600u;
            public const uint B1000000 = 1000000u;
            public const uint B1152000 = 1152000u;
            public const uint B1500000 = 1500000u;
            public const uint B2000000 = 2000000u;
            public const uint B2500000 = 2500000u;
            public const uint B3000000 = 3000000u;
            public const uint B3500000 = 3500000u;
            public const uint B4000000 = 4000000u;
            public const uint B5000000 = 5000000u;
            public const uint B10000000 = 10000000u;

            public const uint SPEED_MAX = uint.MaxValue; // 4294967295U
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WinSize
        {
            public ushort ws_row;    // rows (lines)
            public ushort ws_col;    // columns
            public ushort ws_xpixel; // width in pixels  (often 0)
            public ushort ws_ypixel; // height in pixels (often 0)
        }

        private const string Lib = "libc";

        /// <summary>Return the output baud rate stored in termios.</summary>
        [LibraryImport(Lib)]
        public static partial uint cfgetospeed(in Termios termios_p);

        /// <summary>Return the input baud rate stored in termios.</summary>
        [LibraryImport(Lib)]
        public static partial uint cfgetispeed(in Termios termios_p);

        /// <summary>Set the output baud rate stored in termios.</summary>
        [LibraryImport(Lib)]
        public static partial int cfsetospeed(ref Termios termios_p, uint speed);

        /// <summary>Set the input baud rate stored in termios.</summary>
        [LibraryImport(Lib)]
        public static partial int cfsetispeed(ref Termios termios_p, uint speed);

        /// <summary>Set both input and output baud rates in termios (GNU/MISC).</summary>
        [LibraryImport(Lib)]
        public static partial int cfsetspeed(ref Termios termios_p, uint speed);

        // GNU explicit-numeric baud rate variants (baud_t == speed_t == uint)

        /// <summary>Return the output baud rate as a plain numeric value (GNU).</summary>
        [LibraryImport(Lib)]
        public static partial uint cfgetobaud(in Termios termios_p);

        /// <summary>Return the input baud rate as a plain numeric value (GNU).</summary>
        [LibraryImport(Lib)]
        public static partial uint cfgetibaud(in Termios termios_p);

        /// <summary>Set the output baud rate to a plain numeric value (GNU).</summary>
        [LibraryImport(Lib)]
        public static partial int cfsetobaud(ref Termios termios_p, uint baud);

        /// <summary>Set the input baud rate to a plain numeric value (GNU).</summary>
        [LibraryImport(Lib)]
        public static partial int cfsetibaud(ref Termios termios_p, uint baud);

        /// <summary>Set both baud rates to a plain numeric value (GNU).</summary>
        [LibraryImport(Lib)]
        public static partial int cfsetbaud(ref Termios termios_p, uint baud);

        // ── get / set attributes ──────────────────────────────────────────────

        /// <summary>Get terminal attributes for file descriptor fd.</summary>
        [LibraryImport(Lib)]
        public static partial int tcgetattr(int fd, ref Termios termios_p);

        /// <summary>Set terminal attributes for file descriptor fd.</summary>
        [LibraryImport(Lib)]
        public static partial int tcsetattr(int fd, TcSetAttrAction action, in Termios termios_p);

        /// <summary>Set termios to indicate raw mode (MISC).</summary>
        [LibraryImport(Lib)]
        public static partial void cfmakeraw(ref Termios termios_p);

        /// <summary>Send zero bits (break) on fd for the specified duration.</summary>
        [LibraryImport(Lib)]
        public static partial int tcsendbreak(int fd, int duration);

        /// <summary>Wait for pending output on fd to be transmitted.
        /// (Cancellation point - not marked __THROW in glibc).</summary>
        [LibraryImport(Lib)]
        public static partial int tcdrain(int fd);

        /// <summary>Flush pending data on fd.</summary>
        [LibraryImport(Lib)]
        public static partial int tcflush(int fd, TcFlushQueue queue_selector);

        /// <summary>Suspend or restart transmission on fd.</summary>
        [LibraryImport(Lib)]
        public static partial int tcflow(int fd, TcFlowAction action);

        /// <summary>Get process group ID for session leader for controlling terminal fd.</summary>
        [LibraryImport(Lib)]
        public static partial int tcgetsid(int fd);

        /// <summary>Checks whether the file descriptor is a TTY.
        /// 0 if it's not (0 indicates either not an FD or not a TTY).</summary>
        [LibraryImport(Lib)]
        public static partial int isatty(int fd);


        [LibraryImport(Lib)]
        // glibc/BSD has CULong, musl has int
        public static partial int ioctl(int fd, CULong op, ref WinSize winSize);
        public static uint TIOCGWINSZ
        {
            get
            {
                if (OperatingSystem.IsMacOS())
                    return 0x40087468;

                return 0x5413;
            }
        }
    }
}
