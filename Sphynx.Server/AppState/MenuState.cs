using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sphynx.Core;
using Sphynx.Server.User;
using Sphynx.Utils;

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
            
            SphynxUserManager.UserCreated += OnUserCreated;
            
            Console.Write("Enter username: ");
            string name = Console.ReadLine()?.Trim() ?? string.Empty;
            
            var creationTask = SphynxUserManager.CreateUserAsync(new(name,"zook"));

            Console.WriteLine("\nWaiting for finalizing input...");
            Console.ReadLine();

            SphynxErrorInfo<SphynxUserDbInfo?>? res = null;
            
            try
            {
                 res = creationTask.Result;
            }
            catch(Exception ex)
            {
                Console.WriteLine(res.HasValue ? res.Value.ErrorCode.ToString() : "NoVal");
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("Done creation task!");

            return null;
        }

        private void OnUserCreated(SphynxUserDbInfo obj)
        {
            Console.WriteLine($"\n\n------------\nI saw that that you created a new user, {obj.UserId}, \"{obj.UserName}\"\n------------");
        }
    }
}
