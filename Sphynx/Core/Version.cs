namespace Sphynx.Core
{
    /// <summary>
    /// Represents a version of this application.
    /// </summary>
    public readonly struct Version : IEquatable<Version>, IComparable<Version>
    {
        /// <summary>
        /// The major version number.
        /// </summary>
        public readonly byte Major { get; }

        /// <summary>
        /// The minor version number.
        /// </summary>
        public readonly byte Minor { get; }

        /// <summary>
        /// The patch number for the version.
        /// </summary>
        public readonly byte Patch { get; }

        /// <summary>
        /// Constructs a Version instance within the give major, minor, and patch numbers.
        /// </summary>
        /// <param name="major">The major number for the version.</param>
        /// <param name="minor">The minor number for the version.</param>
        /// <param name="patch">The patch number for the version.</param>
        public Version(byte major, byte minor = 0, byte patch = 0)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
        }

        /// <summary>
        /// Serializes the version into a 32-bit integer.
        /// </summary>
        /// <returns>The version encoded into a 32-bit integer.</returns>
        public readonly int ToInt32() => (Major << 16) | (Minor << 8) | Patch;

        /// <summary>
        /// Constructs a <see cref="Version"/> instance from a serialized 32-bit integer.
        /// </summary>
        /// <param name="ver">The serialized version instance, as a 32-bit integer.</param>
        /// <returns>A new <see cref="Version"/> from the specified <paramref name="ver"/>.</returns>
        public static Version FromInt32(int ver)
        {
            byte major = (byte)((ver >> 16) & 0xff);
            byte minor = (byte)((ver >> 8) & 0xff);
            byte patch = (byte)(ver & 0xff);

            return new Version(major, minor, patch);
        }

        /// <summary>
        /// Checks if two versions are equal.
        /// </summary>
        /// <param name="other">The other version to which this should be compared.</param>
        /// <returns>True if this version is the same as the given one, false otherwise.</returns>
        public readonly bool Equals(Version other) => CompareTo(other) == 0;

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj) => obj is Version version && Equals(version);

        /// <summary>
        /// Converts the version number into a dotted string with the following format:
        ///     <code>MAJOR.MINOR.PATCH</code>
        /// </summary>
        /// <returns>The version number as a string in dotted notation.</returns>
        public readonly override string ToString() => $"{Major}.{Minor}.{Patch}";

        /// <summary>
        /// Computes the hash code for the specific version.
        /// </summary>
        /// <returns>The hash code for the version.</returns>
        public readonly override int GetHashCode() => ToInt32();

        /// <inheritdoc/>
        public readonly int CompareTo(Version other) => ToInt32() - other.ToInt32();

        /// <summary>
        /// Tests equality of two versions.
        /// </summary>
        /// <param name="left">First version to compare.</param>
        /// <param name="right">Second version to compare.</param>
        /// <returns>true if <paramref name="left"/> is equal to <paramref name="right"/>; false otherwise.</returns>
        public static bool operator ==(Version left, Version right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Tests (in)equality of two versions.
        /// </summary>
        /// <param name="left">First version to compare.</param>
        /// <param name="right">Second version to compare.</param>
        /// <returns>true if <paramref name="left"/> is not equal to <paramref name="right"/>; false otherwise.</returns>
        public static bool operator !=(Version left, Version right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Compares the two versions against each other to test which is less.
        /// </summary>
        /// <param name="left">First version to compare.</param>
        /// <param name="right">Second version to compare.</param>
        /// <returns>true if <paramref name="left"/> is less than <paramref name="right"/>; false otherwise.</returns>
        public static bool operator <(Version left, Version right)
        {
            return left.CompareTo(right) < 0;
        }

        /// <summary>
        /// Compares the two versions against each other to test which is less or if they are equal.
        /// </summary>
        /// <param name="left">First version to compare.</param>
        /// <param name="right">Second version to compare.</param>
        /// <returns>true if <paramref name="left"/> is less than or equal to <paramref name="right"/>; false otherwise.</returns>
        public static bool operator <=(Version left, Version right)
        {
            return left.CompareTo(right) <= 0;
        }

        /// <summary>
        /// Compares the two versions against each other to test which is greater.
        /// </summary>
        /// <param name="left">First version to compare.</param>
        /// <param name="right">Second version to compare.</param>
        /// <returns>true if <paramref name="left"/> is greater than <paramref name="right"/>; false otherwise.</returns>
        public static bool operator >(Version left, Version right)
        {
            return left.CompareTo(right) > 0;
        }

        /// <summary>
        /// Compares the two versions against each other to test which is greater or if they are equal.
        /// </summary>
        /// <param name="left">First version to compare.</param>
        /// <param name="right">Second version to compare.</param>
        /// <returns>true if <paramref name="left"/> is greater than or equal to <paramref name="right"/>; false otherwise.</returns>
        public static bool operator >=(Version left, Version right)
        {
            return left.CompareTo(right) >= 0;
        }
    }
}
