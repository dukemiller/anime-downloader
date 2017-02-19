using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace anime_downloader.Classes
{
    // http://stackoverflow.com/questions/3099581/sorting-an-array-of-folder-names-like-windows-explorer-numerically-and-alphabet
    public class WindowsSortComparer : IComparer<string>
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int StrCmpLogicalW(string x, string y);

        public int Compare(string x, string y)
        {
            return StrCmpLogicalW(x, y);
        }
    }
}
