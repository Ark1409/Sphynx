using Spectre.Console;
using Spectre.Console.Rendering;

namespace Sphynx.Client.UI
{
    public class Button : Renderable, IHasBoxBorder, IHasBorder, IPaddable, IFocusable
    {
        public BoxBorder Border { get; set; }
        public bool UseSafeBorder { get; set; }
        public Style? BorderStyle { get; set; } = Color.White;

        public Style? SelectedBorderStyle { get; set; }

        public bool Selected { get; set; }
        public Padding? Padding { get; set; }

        public string Text { get; set; }

        public Style? TextStyle { get; set; }

        public Style? SelectedTextStyle { get; set; }

        public event Action OnClick;

        public Button() { }

        public Button(string text, Style? style = null, Style? selectedStyle = null)
        {
            Text = text;
            TextStyle = style;
            SelectedTextStyle = selectedStyle;
        }

        internal Measurement DoMeasure(RenderOptions options, int maxWidth)
        {
            int maxLineLen = 0;
            int lineBegin = 0;
            for (int i = 0; i < Text.Length; i++)
            {
                if (Text[i] is '\n')
                {
                    maxLineLen = Math.Max(maxLineLen, i - lineBegin);
                    lineBegin = i + 1;
                }
            }
            maxLineLen = Math.Max(maxLineLen, Text.Length - lineBegin);
            int finalWidth = maxLineLen + (Border == BoxBorder.None ? 0 : 2);
            return new Measurement(Math.Min(finalWidth, maxWidth), Math.Min(finalWidth, maxWidth));
        }

        protected override Measurement Measure(RenderOptions options, int maxWidth) => DoMeasure(options, maxWidth);

        internal IEnumerable<Segment> DoRender(RenderOptions options, int maxWidth)
        {
            // Does not pay attention to height whatsoever
            int trueWidth = Math.Min(maxWidth, Measure(options, maxWidth).Min);
            int trueHeight = (options.Height ?? int.MaxValue) - (Border == BoxBorder.None ? 0 : 2);
            if (trueWidth <= 0 || trueHeight <= 0) return Enumerable.Empty<Segment>();

            string outputText = Text.Trim();
            int lineCount = 1;
            for (int i = 0; i < outputText.Length; i++)
            {
                if (outputText[i] == '\n')
                {
                    lineCount++;
                }

                if (lineCount > trueHeight)
                {
                    outputText = outputText.Substring(0, i);
                    lineCount--;
                    break;
                }
            }
            trueHeight = Math.Min(trueHeight, lineCount);

            var para = new CharacterWrapParagraph(outputText, (Selected ? SelectedTextStyle : TextStyle) ?? Style.Plain);
            para.Width = Math.Max(0, trueWidth - 2);
            para.Ellipsis();
            var p = new Panel(para)
            {
                Border = Border ?? BoxBorder.Square,
                UseSafeBorder = UseSafeBorder,
                BorderStyle = (Selected ? SelectedBorderStyle : BorderStyle) ?? Color.White,
                Padding = Padding ?? new(0, 0, 0, 0)
            };
            p.Width = Math.Max(2, trueWidth);
            p.Collapse();
            return p.GetSegments(AnsiConsole.Console);
        }

        protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth) => DoRender(options, maxWidth);

        public bool HandleKey(in ConsoleKeyInfo key)
        {
            switch (key.KeyChar)
            {
                case '\r':
                case '\n':
                    OnClick?.Invoke();
                    return true;
                default: break;
            }
            return false;
        }

        public void OnFocus()
        {
            Selected = true;
        }

        public void OnLeave()
        {
            Selected = false;
        }
    }
}
