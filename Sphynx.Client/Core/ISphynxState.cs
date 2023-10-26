using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Sphynx.Client.Core
{
    internal interface ISphynxState
    {
        ISphynxState? Run();
    }

    internal class SphynxLobbyState : ISphynxState
    {
        private SphynxUser _user;

        public SphynxLobbyState(SphynxUser user)
        {
            _user = user;
        }

        public ISphynxState? Run()
        {
            ClearConsole();
            Console.WriteLine($"Lobby state for {_user.Name}");
            Thread.Sleep(3000);
            return null;
        }

        private void ClearConsole()
        {
            Console.Clear();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.ForegroundColor = Console.ForegroundColor == Console.BackgroundColor ? (ConsoleColor)((int)Console.ForegroundColor + 1) : Console.ForegroundColor;

            Console.WriteLine(SphynxClient.GetHeader());
        }
    }

    internal class SphynxLoginState : ISphynxState
    {
        public SphynxUser? User { get; private set; }

        public ISphynxState? Run()
        {
            while (true)
            {
            Username:
                ClearConsole();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Enter your username: ");
                string username = Console.ReadLine()!;
                for (; string.IsNullOrEmpty(username); username = Console.ReadLine()!) ;

                const uint MAX_ATTEMPTS = 5;
                int waitTime = 5;
                for (ClearConsole(); ; ClearConsole(), waitTime *= 2)
                {
                    for (uint @try = 0; @try < MAX_ATTEMPTS; @try++)
                    {
                        Console.WriteLine($"User: {username}\n");
                        Console.Write("Enter your password: ");

                        string? password = ReadPassword();

                        if (!string.IsNullOrEmpty(password) && SphynxUser.ValidateUser(username, password))
                        {
                            this.User = SphynxUser.GetUser(username);
                            goto StatusCheck;
                        }

                        ClearConsole();
                        Console.WriteLine("Incorrect password. Please try again...\n");
                    }
                    Console.WriteLine($"Too many failed attempts. Please wait {waitTime} seconds before trying again...");

                    uint barWidth = (uint)Math.Min(waitTime * 3, Console.BufferWidth - "[] 100s".Length);
                    for (DateTime beginWait = DateTime.Now, currenTime = DateTime.Now; currenTime.Subtract(beginWait).TotalSeconds < waitTime; currenTime = DateTime.Now)
                    {
                        double diff = currenTime.Subtract(beginWait).TotalSeconds;
                        WriteLoadingBar(barWidth, diff / waitTime);
                        Console.Write((uint)diff + "s");
                        Thread.Sleep(32);
                    }
                }

            StatusCheck:
                if (this.User?.Status != SphynxUser.UserStatus.OFFLINE)
                {
                    ClearConsole();
                    Console.WriteLine($"User {User?.Name} is already logged in on a different machine!\n");
                    Console.WriteLine("Retrying in...");
                    const uint redoLoginWaitTime = 3;
                    uint barWidth = (uint)(Console.BufferWidth - "[] 100s".Length) / 3;
                    for (DateTime beginWait = DateTime.Now, currenTime = DateTime.Now; currenTime.Subtract(beginWait).TotalSeconds < redoLoginWaitTime; currenTime = DateTime.Now)
                    {
                        double diff = currenTime.Subtract(beginWait).TotalSeconds;
                        WriteLoadingBar(barWidth, diff / redoLoginWaitTime);
                        Console.Write((uint)diff + "s");
                        Thread.Sleep(32);
                    }
                    goto Username;
                }
                else
                {
                    goto End;
                }
            }
        End:
            return new SphynxLobbyState(this.User);
        }
        private void ClearConsole()
        {
            Console.Clear();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.ForegroundColor = Console.ForegroundColor == Console.BackgroundColor ? (ConsoleColor)((int)Console.ForegroundColor + 1) : Console.ForegroundColor;

            Console.WriteLine(SphynxClient.GetHeader());
        }

        private string? ReadPassword()
        {
            var sb = new StringBuilder();

            for (char c = Console.ReadKey(true).KeyChar; c != '\r' && c != '\n'; c = Console.ReadKey(true).KeyChar)
            {
                if (c == '\b')
                {
                    if (sb.Length > 0)
                    {
                        sb.Remove(sb.Length - 1, 1);
                        Console.Write("\b \b");
                    }
                }
                else if (!IsLatin1Printable(c))
                {
                    var currentForeground = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(" <Aborted>");
                    Console.ForegroundColor = currentForeground;

                    Thread.Sleep(200);
                    return null;
                }
                else
                {
                    sb.Append(c);
                    Console.Write('*');
                }
            }
            Console.WriteLine();
            return sb.ToString();
        }

        private bool IsLatin1Printable(char ch) => (ch >= '\x20' && ch <= '\x7E') || (ch >= '\xA0' && ch <= '\xFF');

        private void WriteLoadingBar(uint width, double t)
        {
            Console.Write("\r[");
            var sb = new StringBuilder();
            for (uint i = 0; i < width; i++)
            {
                sb.Append(i * 100 / width < 100 * t ? '#' : ' ');
            }
            Console.Write(sb.ToString());
            Console.Write("] ");
        }
    }
}
