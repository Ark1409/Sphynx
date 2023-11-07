using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphynx.Core
{
    public struct Version : IEquatable<Version?>
    {
        public byte Major { get; private set; }

        public byte Minor { get; private set; }

        public byte Patch { get; private set; }

        public Version(byte major, byte minor = 0, byte patch = 0)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
        }

        public override readonly int GetHashCode() => ToInt32();

        public readonly int ToInt32() => Major << 16 | Minor << 8 | Patch;

        public static Version FromInt32(int ver) => new Version((byte)(ver & 0xff0000 >> 16), (byte)(ver & 0xff00 >> 8), (byte)(ver & 0xff));

        public override readonly string ToString() => Major.ToString() + "." + Minor.ToString() + Patch.ToString();

        public readonly bool Equals(Version? other) => Major == other?.Major && Minor == other?.Minor && Patch == other?.Patch;
    }
}
