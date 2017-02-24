using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace anime_downloader.Models
{
    public class SemanticVersion
    {
        public int Major { get; set; }

        public int Minor { get; set; }

        public int Patch { get; set; }

        public SemanticVersion() { }

        public SemanticVersion(string text)
        {
            var split = Regex.Split(text, @"\.");
            Major = int.Parse(split[0]);
            Minor = int.Parse(split[1]);
            if (split[2].All(char.IsNumber))
                Patch = int.Parse(split[2]);
        }

        private int TotalValue => Major + Minor + Patch;

        public static bool operator <(SemanticVersion left, SemanticVersion right)
        {
            return left.TotalValue < right.TotalValue;
        }

        public static bool operator >(SemanticVersion left, SemanticVersion right)
        {
            return left.TotalValue > right.TotalValue;
        }

        public static bool operator <=(SemanticVersion left, SemanticVersion right)
        {
            return left.TotalValue <= right.TotalValue;
        }

        public static bool operator >=(SemanticVersion left, SemanticVersion right)
        {
            return left.TotalValue >= right.TotalValue;
        }

        public override string ToString() => $"{Major}.{Minor}.{Patch}";
    }
}
