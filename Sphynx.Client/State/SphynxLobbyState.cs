using Sphynx.Core;

namespace Sphynx.Client.State
{
    internal class SphynxLobbyState : ISphynxState
    {
        private SphynxSessionUser _user;
        private SphynxApp _app;

        public SphynxLobbyState(SphynxApp app, SphynxSessionUser user)
        {
            _app = app;
            _user = user;
        }

        public ISphynxState? Run()
        {
            ClearConsole();
            Console.WriteLine($"Lobby state for {_app.Server!.GetUsername(_user)}");
            Thread.Sleep(5000);
            return null;
        }

        private void ClearConsole()
        {
            Console.Clear();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.ForegroundColor = Console.ForegroundColor == Console.BackgroundColor ? (ConsoleColor)((int)Console.ForegroundColor + 1) : Console.ForegroundColor;

            Console.WriteLine(SphynxApp.GetHeader());
        }

        public void Dispose()
        {
            
        }
    }
}
