using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphynx.Server.AppState
{
    public sealed class MenuState : ISphynxState
    {
        public ISphynxState? Run()
        {
            Console.WriteLine("#######################################################");
            Console.WriteLine("##                   SPHYNX SERVER                   ##");
            Console.WriteLine("#######################################################");
            Console.WriteLine();

            Thread.Sleep(1000 / 30);
            Console.CursorLeft = 0;
            Console.CursorTop = Console.WindowHeight - 1;
            Console.Write($"Running server @ {SphynxApp.Server!.EndPoint}.");

            Console.ReadLine();

            return null;
        }
    }
}
