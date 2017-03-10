using System;
using System.Linq;
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
        private readonly int _major;

        private readonly int _minor;

        private readonly int _patch;

        // 

        public SemanticVersion() { }

        private SemanticVersion(int major, int minor, int patch)
        {
            _major = major;
            _minor = minor;
            _patch = patch;
        }

        private SemanticVersion(Tuple<int, int, int> version) 
            : this(version.Item1, version.Item2, version.Item3) { }

        public SemanticVersion(string version): this(ParseString(version)) { }

        public SemanticVersion(Version version) : this(version.ToString()) { }

        // 

        public static bool operator <(SemanticVersion left, SemanticVersion right)
        {
            return left._major < right._major 
                || left._major == right._major && left._minor < right._minor 
                || left._major == right._major && left._minor == right._minor && left._patch < right._patch;
        }

        public static bool operator >(SemanticVersion left, SemanticVersion right)
        {
            return left._major > right._major
                || left._major == right._major && left._minor > right._minor
                || left._major == right._major && left._minor == right._minor && left._patch > right._patch;
        }

        public override string ToString() => $"{_major}.{_minor}.{_patch}";

        // 

        private static Tuple<int, int, int> ParseString(string text)
        {
            int major = 0, minor = 0, patch = 0;
            var split = Regex.Split(text, @"\.");

            if (split.Length < 3)
                return Tuple.Create(major, minor, patch);

            int.TryParse(split[0], out major);
            int.TryParse(split[1], out minor);
            if (split[2].All(char.IsNumber))
                int.TryParse(split[2], out patch);

            return Tuple.Create(major, minor, patch);
        }
    }
}
