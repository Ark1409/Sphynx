using Mindmagma.Curses;

namespace Sphynx.Client
{
    internal static class Program
    {
        static int Main(string[] args)
        {
            var scr = NCurses.InitScreen();
            Console.WriteLine("Hello World");
            NCurses.WindowAddString(scr, "Hello World");
            NCurses.Refresh();
            NCurses.Raw();
            NCurses.GetChar();
            NCurses.EndWin();
            return 0;
        }
    }
}
