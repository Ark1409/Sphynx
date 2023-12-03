using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphynx.Core
{
    public struct Version : IEquatable<Version?>, IComparable<Version>
    {
        /// <summary>
        /// The major version number
        /// </summary>
        public byte Major { get; private set; }

        /// <summary>
        /// The minor version number
        /// </summary>
        public byte Minor { get; private set; }

        /// <summary>
        /// The patch number for the version
        /// </summary>
        public byte Patch { get; private set; }

        /// <summary>
        /// Constructs a Version instancce within the give major, minor, and patch numbers
        /// </summary>
        /// <param name="major">The major number for the version</param>
        /// <param name="minor">The minor number for the version</param>
        /// <param name="patch">The patch number for the version</param>
        public Version(byte major, byte minor = 0, byte patch = 0)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
        }

        /// <summary>
        /// Computes the hash code for the specific version
        /// </summary>
        /// <returns>The hash code for the version</returns>
        public override readonly int GetHashCode() => ToInt32();

        /// <summary>
        /// Serializes the version into a 32-bit integer
        /// </summary>
        /// <returns>The version encoded into a 32-bit integer</returns>
        public readonly int ToInt32() => Major << 16 | Minor << 8 | Patch;

        /// <summary>
        /// Constructs a version class instance from a serialized 32-bit integer
        /// </summary>
        /// <param name="ver">The serialized version instance, as a 32-bit integer</param>
        /// <returns>An instance of the Version class ob</returns>
        public static Version FromInt32(int ver) => new Version((byte)(ver >> 16 & 0xff), (byte)(ver >> 8 & 0xff), (byte)(ver & 0xff));

        /// <summary>
        /// Converts the version number into a dotted string with the following format:
        /// <code>MAJOR.MINOR.PATCH</code>
        /// </summary>
        /// <returns>The version number as a string in dotted notation.</returns>
        public override readonly string ToString() => Major.ToString() + "." + Minor.ToString() + "." + Patch.ToString();

        /// <summary>
        /// Checks if two versions are equal.
        /// </summary>
        /// <param name="other">The other version to which this should be comapred.</param>
        /// <returns>True if this version is the same as the given one, false otherwise.</returns>
        public readonly bool Equals(Version? other) => other.HasValue && CompareTo(other.Value) == 0;

        /// <inheritdoc/>
        public readonly int CompareTo(Version other) => ToInt32() - other.ToInt32();

        public static bool operator ==(Version left, Version right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Version left, Version right)
        {
            return !(left == right);
        }

        public static bool operator <(Version left, Version right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator <=(Version left, Version right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >(Version left, Version right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator >=(Version left, Version right)
        {
            return left.CompareTo(right) >= 0;
        }
    }
}
