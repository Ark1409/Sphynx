using System.Collections;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Sphynx.Client.UI
{
    internal class Aligner : Renderable
    {
        public IRenderable Content { get; set; }
        public HorizontalAlignment Horizontal { get; set; }
        public VerticalAlignment? Vertical { get; set; }

        public int? Width { get; set; }
        public int? Height { get; set; }

        public Aligner(IRenderable content, HorizontalAlignment horizontal, VerticalAlignment? vertical = null)
        {
            Content = content;
            Horizontal = horizontal;
            Vertical = vertical;
        }

        public static Aligner Center(IRenderable content, VerticalAlignment? vertical = null) => new(content, HorizontalAlignment.Center, vertical);
        public static Aligner Left(IRenderable content, VerticalAlignment? vertical = null) => new(content, HorizontalAlignment.Left, vertical);
        public static Aligner Right(IRenderable content, VerticalAlignment? vertical = null) => new(content, HorizontalAlignment.Right, vertical);
        
        internal Measurement DoMeasure(RenderOptions options, int maxWidth)
        {
            var childMeasure = Content.Measure(options, maxWidth);
            int trueWidth = Math.Min(Width ?? maxWidth, maxWidth);
            return new Measurement(Math.Max(trueWidth, childMeasure.Min), Math.Max(trueWidth, childMeasure.Max));
        }

        protected override Measurement Measure(RenderOptions options, int maxWidth) => DoMeasure(options, maxWidth);

        internal IEnumerable<Segment> DoRender(RenderOptions options, int maxWidth)
        {
            Vertical ??= VerticalAlignment.Top;

            var childSegments = Content.Render(options, maxWidth);
            var lines = Segment.SplitLines(childSegments);

            var measure = Measure(options, maxWidth);
            
            int trueWidth = measure.Min;
            int trueHeight = options.Height.HasValue ? Math.Min(options.Height!.Value, Height ?? options.Height!.Value) : Math.Max(lines.Count, Height ?? lines.Count);

            var blank = new SegmentLine(new[] { new Segment(new string(' ', trueWidth)) });
            
            switch (Vertical)
            {
                case VerticalAlignment.Top:
                    {
                        var diff = trueHeight - lines.Count;
                        for (var i = 0; i < diff; i++)
                        {
                            lines.Add(blank);
                        }

                        break;
                    }

                case VerticalAlignment.Middle:
                    {
                        var top = (trueHeight - lines.Count) / 2;
                        var bottom = trueHeight - top - lines.Count;

                        for (var i = 0; i < top; i++)
                        {
                            lines.Insert(0, blank);
                        }

                        for (var i = 0; i < bottom; i++)
                        {
                            lines.Add(blank);
                        }

                        break;
                    }

                case VerticalAlignment.Bottom:
                    {
                        var diff = trueHeight - lines.Count;
                        for (var i = 0; i < diff; i++)
                        {
                            lines.Insert(0, blank);
                        }

                        break;
                    }
            }

            int currentWidth = Content.Measure(options, maxWidth).Min;
            foreach (var segments in lines)
            {
                switch (Horizontal)
                {
                    case HorizontalAlignment.Left:
                        {
                            var diff = trueWidth - currentWidth;
                            segments.Add(Segment.Padding(diff));
                            break;
                        }

                    case HorizontalAlignment.Right:
                        {
                            var diff = trueWidth - currentWidth;
                            segments.Insert(0, Segment.Padding(diff));
                            break;
                        }

                    case HorizontalAlignment.Center:
                        {
                            // Left side.
                            var diff = (trueWidth - currentWidth) / 2;
                            segments.Insert(0, Segment.Padding(diff));

                            // Right side
                            segments.Add(Segment.Padding(diff));
                            var remainder = (trueWidth - currentWidth) % 2;
                            if (remainder != 0)
                            {
                                segments.Add(Segment.Padding(remainder));
                            }

                            break;
                        }
                }
            }
            return new SegmentLineEnumerable(lines);
        }

        protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth) => DoRender(options, maxWidth);

        private class SegmentLineEnumerable : IEnumerable<Segment>
        {
            private List<Segment> _segments = new();

            internal SegmentLineEnumerable(IEnumerable<SegmentLine> segments)
            {
                var newLine = new Segment("\n");
                foreach (var segmentLine in segments)
                {
                    _segments.AddRange(segmentLine);
                    _segments.Add(newLine);
                }
            }

            public IEnumerator<Segment> GetEnumerator() => _segments.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
