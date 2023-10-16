using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphynx.Client.Core
{
    public class SphynxClient
    {
        private ISphynxState? State { get; set; }
        private void Init()
        {
            State = new SphynxLoginState();
        }

        private void Cleanup()
        {
            State = null;
        }

        public void Start()
        {
            Init();

            var t = new Thread(Run);
            t.Start();
            t.Join();

            Cleanup();
        }



        private void Run()
        {
            while (State != null)
            {
                State = State.Run();
            }
        }


    }
}
