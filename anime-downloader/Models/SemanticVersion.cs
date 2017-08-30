using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace anime_downloader.Models
{
    /// <summary>
    ///     Immutable class representing a semantic version of only three significant values.
    /// </summary>
    /// <remarks>
    ///     The built-in version was a little less flexible and had some quirks
    /// </remarks>
    public class SemanticVersion
    {
        
        /// <summary>
        ///     The semantic version of the currently executing assembly
        /// </summary>
        public static SemanticVersion Application => new SemanticVersion(Assembly.GetExecutingAssembly().GetName().Version);

        // Constructors

        public SemanticVersion() { }

        public SemanticVersion(int major, int minor, int patch)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
        }

        public SemanticVersion((int major, int minor, int patch) version) 
            : this(version.major, version.minor, version.patch) { }

        public SemanticVersion(string version): this(ParseVersion(version)) { }

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
            return left?.Major == right?.Major && left?.Minor == right?.Minor && left?.Patch == right?.Patch;
        }

        public static bool operator !=(SemanticVersion left, SemanticVersion right)
        {
            return !(left == right);
        }

        protected bool Equals(SemanticVersion other)
        {
            return Major == other.Major && Minor == other.Minor && Patch == other.Patch;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SemanticVersion)obj);
        }

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

        // 

        private static (int, int, int) ParseVersion(string text)
        {
            int major = 0, minor = 0, patch = 0;
            var match = Regex.Match(text, @"((\d+)\.(\d+).(\d+))");
            if (match.Success)
            {
                int.TryParse(match.Groups[2].Value, out major);
                int.TryParse(match.Groups[3].Value, out minor);
                int.TryParse(match.Groups[4].Value, out patch);
            }
            return (major, minor, patch);
        }
    }
}
