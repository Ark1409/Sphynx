using Spectre.Console;

namespace Sphynx.Client.UI
{
    public class TextField : TextBox
    {
        public override int? Height
        {
            get => 1;
            set => throw new InvalidOperationException();
        }

        public TextField() { }
        public TextField(in ValueTuple<string, Style> text) : base(text) { }
        public TextField(in ValueTuple<char, Style> text) : base(text) { }
        public TextField(string text, Style? style = null) : base(text, style) { }
        public TextField(IEnumerable<ValueTuple<string, Style>> texts) : base(texts) { }

        public override bool HandleKey(in ConsoleKeyInfo key)
        {
            if (key.KeyChar is '\r' or '\n') return false;
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                case ConsoleKey.DownArrow:
                case ConsoleKey.PageUp:
                case ConsoleKey.PageDown:
                    return false;
                default: break;
            }
            return base.HandleKey(key);
        }
    }
}
