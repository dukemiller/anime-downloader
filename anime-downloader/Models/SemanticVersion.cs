using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace anime_downloader.Models
{
    /// <summary>
    ///     Immutable struct representing a semantic version of only three significant values.
    /// </summary>
    public readonly struct SemanticVersion
    {

        public static SemanticVersion Application => new SemanticVersion(Assembly.GetExecutingAssembly().GetName().Version);

        // Constructors
        
        public SemanticVersion(int major, int minor, int patch)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
        }

        public SemanticVersion(string version) 
        {
            int major = 0, minor = 0, patch = 0;
            var match = Regex.Match(version, @"((\d+)\.(\d+)\.(\d+))");
            if (match.Success)
            {
                int.TryParse(match.Groups[2].Value, out major);
                int.TryParse(match.Groups[3].Value, out minor);
                int.TryParse(match.Groups[4].Value, out patch);
            }

            Major = major;
            Minor = minor;
            Patch = patch;
        }

        public SemanticVersion(Version version) : this(version.ToString()) { }

        // Properties

        public int Major { get; }

        public int Minor { get; }

        public int Patch { get; }

        // Operators

        public static bool operator <(SemanticVersion left, SemanticVersion right)
        {
            return left.Major < right.Major 
                || left.Major == right.Major && left.Minor < right.Minor 
                || left.Major == right.Major && left.Minor == right.Minor && left.Patch < right.Patch;
        }

        public static bool operator >(SemanticVersion left, SemanticVersion right)
        {
            return left.Major > right.Major
                || left.Major == right.Major && left.Minor > right.Minor
                || left.Major == right.Major && left.Minor == right.Minor && left.Patch > right.Patch;
        }

        public static bool operator ==(SemanticVersion left, SemanticVersion right)
        {
            return left.Major == right.Major && left.Minor == right.Minor && left.Patch == right.Patch;
        }

        public static bool operator !=(SemanticVersion left, SemanticVersion right) => !(left == right);

        public bool Equals(SemanticVersion other) => Major == other.Major && Minor == other.Minor && Patch == other.Patch;

        public override bool Equals(object obj) => !(obj is null) && obj is SemanticVersion other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Major;
                hashCode = (hashCode * 397) ^ Minor;
                hashCode = (hashCode * 397) ^ Patch;
                return hashCode;
            }
        }

        // Explicit conversions

        public Version ToVersion() => new Version(ToString());

        public override string ToString() => $"{Major}.{Minor}.{Patch}";
    }
}
