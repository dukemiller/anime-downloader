using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using anime_downloader.Models;
using GalaSoft.MvvmLight;
using static anime_downloader.Classes.Methods;

namespace anime_downloader.ViewModels.Components
{
    public class NotesViewModel: ViewModelBase
    {
        public NotesViewModel() => Load();

        // 

        public List<ReleaseNote> Notes { get; set; } = new List<ReleaseNote>();

        // 

        private async void Load()
        {
            if (IsInDesignMode)
                return;

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("anime_downloader.Resources.Text.commits.txt"))
                if (stream != null)
                    using (var reader = new StreamReader(stream))
                    {
                        var text = (await reader.ReadToEndAsync()).Split('\n').Select(t => t.Trim()).Where(Not<string>(string.IsNullOrEmpty)).ToList();
                        var releases = text.Select(t => new ReleaseNote { Date = t.Split('T')[0], Subject = string.Join(" ", t.Split(' ').Skip(1)) }).ToList();
                        for (var i = 0; i < releases.Count; i++)
                        {
                            if (i == 0 || i > 0 && releases[i].Date != releases[i - 1].Date)
                                releases[i].Bold = true;
                        }
                        Notes = releases;
                    }
        }
    }
}
