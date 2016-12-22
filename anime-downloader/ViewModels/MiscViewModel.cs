using System.Collections.Generic;
using System.Linq;
using System.Web;
using anime_downloader.Classes;
using anime_downloader.Services;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace anime_downloader.ViewModels
{
    public class MiscViewModel : ViewModelBase
    {
        private int _selectedIndex;

        public MiscViewModel(IAnimeService animes, IAnimeFileService fileService, IMyAnimeListService malService)
        {
            Animes = animes;
            FileService = fileService;
            MalService = malService;
            SelectedIndex = 0;
            SubmitCommand = new RelayCommand(DoAction);
        }

        public IAnimeService Animes { get; set; }
        public IAnimeFileService FileService { get; set; }
        public IMyAnimeListService MalService { get; set; }

        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set { Set(() => SelectedIndex, ref _selectedIndex, value); }
        }

        public RelayCommand SubmitCommand { get; set; }

        // 

        private async void DoAction()
        {
            // Mark fully watched as completed
            if (SelectedIndex == 1)
            {
                var names = new List<string>();
                foreach (var anime in Animes
                    .AiringAndWatching
                    .Where(
                        a =>
                            a.MyAnimeList.HasId &&
                            (((a.MyAnimeList.OverallTotal > 0) && (a.IntEpisode() == a.MyAnimeList.OverallTotal)) ||
                             ((a.MyAnimeList.TotalEpisodes > 0) && (a.IntEpisode() == a.MyAnimeList.TotalEpisodes)))))
                {
                    anime.Status = "Finished";
                    anime.Airing = false;
                    names.Add(anime.Title);
                }

                var result = names.Count > 0 ? string.Join(", ", names) : "no shows";
                Methods.Alert($"Marked {result} as finished. ");
            }

            // Move duplicates to My Videos
            else if (SelectedIndex == 2)
            {
                var moveCount = FileService.MoveDuplicatesAsync();
                Methods.Alert($"Moved {moveCount} files to duplicate folder.");
            }

            // Regather shows with no ep. total
            else if (SelectedIndex == 3)
            {
                var updated = new List<string>();

                var needsUpdating = Animes
                    .AiringAndWatching
                    .Where(a => a.MyAnimeList.HasId && (a.MyAnimeList.TotalEpisodes == 0))
                    .ToList();

                foreach (var anime in needsUpdating)
                {
                    var results = await MalService.Find(HttpUtility.UrlEncode(anime.Title));
                    var closest = results.FirstOrDefault(r => r.Id.Equals(anime.MyAnimeList.Id));
                    if ((closest != null) && !anime.MyAnimeList.TotalEpisodes.Equals(closest.TotalEpisodes))
                    {
                        updated.Add(anime.Title);
                        anime.MyAnimeList.TotalEpisodes = closest.TotalEpisodes;
                    }
                }

                if (updated.Count > 0)
                {
                    var updateResult = string.Join(", and ", updated);
                    Methods.Alert($"Updated total episodes for {updateResult}.");
                }

                else
                    Methods.Alert($"No shows were updated for an attempted {needsUpdating.Count} shows.");
            }
        }

        // 
    }
}