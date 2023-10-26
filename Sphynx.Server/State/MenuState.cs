using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphynx.Server.State
{
    public sealed class MenuState : ISphynxState
    {
        public ISphynxState? Run()
        {
            Console.WriteLine("#######################################################");
            Console.WriteLine("##                   SPHYNX SERVER                   ##");
            Console.WriteLine("#######################################################");
            Console.WriteLine();
            Console.ReadLine();

            return null;
        }
    }
}
