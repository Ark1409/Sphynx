using Spectre.Console;

namespace Sphynx.Client.UI
{
    public static class ColorExtensions
    {
        public static Color FromRGB(int rgb)
            => new Color((byte)((rgb >> 16) & 0xff),
                (byte)((rgb >> 8) & 0xff),
                (byte)((rgb >> 0) & 0xff));

        public static Color FromHex(string hex) => FromRGB(Convert.ToInt32(hex, 16));
    }
}
