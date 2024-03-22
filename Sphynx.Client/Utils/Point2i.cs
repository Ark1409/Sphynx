namespace Sphynx.Client.Utils
{
    public record struct Point2i(int X, int Y)
    {
        public Point2i Add(Point2i other)
        {
            X += other.X;
            Y += other.Y;
            return this;
        }

        public Point2i Add(int otherX, int otherY) => Add(new Point2i(otherX, otherY));

        public Point2i Subtract(Point2i other)
        {
            X -= other.X;
            Y -= other.Y;
            return this;
        }

        public Point2i Subtract(int otherX, int otherY) => Subtract(new Point2i(otherX, otherY));

        public static Point2i operator +(Point2i left, Point2i right) => left.Add(right);
        public static Point2i operator -(Point2i left, Point2i right) => left.Subtract(right);

        public static Point2i Zero => new(0, 0);

        public static Point2i Empty => Zero;
    }
}
