using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Sphynx.Client.Core;
using Sphynx.Core;

namespace Sphynx.Client.State
{
    internal class SphynxLoginState : ISphynxState
    {
        public SphynxSessionUser? User { get; private set; }

        private readonly SphynxClient _client;

        public SphynxLoginState(SphynxClient client)
        {
            _client = client;
        }

        public ISphynxState? Run()
        {
            while (true)
            {
            Username:
                ClearConsole();

                Console.ForegroundColor = ConsoleColor.Green;

                string username;
                do
                {
                    Console.Write("Enter your username: ");
                } while (string.IsNullOrEmpty(username = Console.ReadLine()!));

                // Constant for holding max attempts until user must wait waitTime seconds
                const int MAX_ATTEMPTS = 5;

                // Holds the time the user will have to wait for if they fail MAX_ATTEMPTS times in a row
                int waitTime = 5;

                // Double the wait time each time they fail MAX_ATTEMPTS times in a row
                for (ClearConsole(); ; ClearConsole(), waitTime *= 2)
                {
                    for (int @try = 0; @try < MAX_ATTEMPTS; @try++)
                    {
                        Console.WriteLine($"User: {username}\n");
                        Console.Write("Enter your password: ");
                        string? password = ReadPassword();

                        if (string.IsNullOrEmpty(password))
                        {
                            goto Username;
                        }

                        User = _client.Server!.ConnectAs(username, password, out var err);

                        switch (err)
                        {
                            case SphynxServer.ErrorCode.OK:
                                goto End;
                            case SphynxServer.ErrorCode.ALREADY_ONLINE:
                                {
                                    ClearConsole();
                                    {
                                        var currentForeground = Console.ForegroundColor;
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine($"User '{username}' is already logged in on a different machine!\n");
                                        Console.ForegroundColor = currentForeground;
                                    }
                                    Console.WriteLine("Retrying in...");
                                    const int redoLoginWaitTime = 5;
                                    int redoBarWidth = (Console.BufferWidth - "[] 100s".Length) / 3;
                                    for (DateTime beginWait = DateTime.Now, currenTime = DateTime.Now; currenTime.Subtract(beginWait).TotalSeconds < redoLoginWaitTime; currenTime = DateTime.Now)
                                    {
                                        double diff = currenTime.Subtract(beginWait).TotalSeconds;
                                        WriteLoadingBar(redoBarWidth, diff / redoLoginWaitTime);
                                        Console.Write((int)(redoLoginWaitTime - diff + 1) + "s");
                                        Thread.Sleep(32);
                                    }

                                    goto Username;
                                }
                            case SphynxServer.ErrorCode.INCORRECT_PASSWORD:
                                {
                                    if (@try < MAX_ATTEMPTS - 1)
                                    {
                                        ClearConsole();
                                        Console.WriteLine("Incorrect password. Please try again...\n");
                                    }
                                    break;
                                }
                            default:
                                {
                                    {
                                        ClearConsole();
                                        var currentForeground = Console.ForegroundColor;
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine("An error occured when trying to connect to the server...\n");
                                        Console.ForegroundColor = currentForeground;
                                    }

                                    Console.WriteLine("Retrying in...");
                                    const int redoLoginWaitTime = 2;
                                    int redoBarWidth = (Console.BufferWidth - "[] 100s".Length) / 3;
                                    for (DateTime beginWait = DateTime.Now, currenTime = DateTime.Now; currenTime.Subtract(beginWait).TotalSeconds < redoLoginWaitTime; currenTime = DateTime.Now)
                                    {
                                        double diff = currenTime.Subtract(beginWait).TotalSeconds;
                                        WriteLoadingBar(redoBarWidth, diff / redoLoginWaitTime);
                                        Console.Write((int)(redoLoginWaitTime - diff + 1) + "s");
                                        Thread.Sleep(32);
                                    }
                                    goto Username;
                                }
                                
                        }
                    }
                    ClearConsole();
                    Console.WriteLine($"Too many failed attempts. Please wait {waitTime} second{(waitTime == 1 ? string.Empty : "s")} before trying again...");

                    int barWidth = Math.Min(waitTime * 3, Console.BufferWidth - "[] 100s".Length);
                    for (DateTime beginWait = DateTime.Now, currenTime = DateTime.Now; currenTime.Subtract(beginWait).TotalSeconds < waitTime; currenTime = DateTime.Now)
                    {
                        double diff = currenTime.Subtract(beginWait).TotalSeconds;
                        WriteLoadingBar(barWidth, diff / waitTime);
                        Console.Write((int)diff + "s");
                        Thread.Sleep(32);
                    }
                }
            }
        End:
            return new SphynxLobbyState(_client, User!);
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

        private bool IsLatin1Printable(char ch) => ch >= '\x20' && ch <= '\x7E' || ch >= '\xA0' && ch <= '\xFF';

        /// <summary>
        /// Writes a loading bar on the current line with the specified width
        /// </summary>
        /// <param name="width">The width of the loading bar, representing how many hash tags the bar will contain (overall width is always at least two characters)</param>
        /// <param name="t">A value between [0, 1] representing the progress of the loading bar</param>
        private void WriteLoadingBar(int width, double t)
        {
            Console.Write("\r[");
            var sb = new StringBuilder();
            for (int i = 0; i < width; i++)
            {
                sb.Append(i * 100 / width < 100 * t ? '#' : ' ');
            }
            Console.Write(sb.ToString());
            Console.Write("] ");
        }
    }
}
