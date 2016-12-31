using System.Collections.Generic;
using System.Linq;
using System.Web;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Services;
using anime_downloader.Services.Interfaces;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace anime_downloader.ViewModels
{
    public class MiscViewModel : ViewModelBase
    {
        private int _selectedIndex;

        public MiscViewModel(ISettingsService settings, IAnimeAggregateService animeAggregate)
        {
            Settings = settings;
            AnimeAggregate = animeAggregate;
            SelectedIndex = 0;
            SubmitCommand = new RelayCommand(DoAction);
        }

        private ISettingsService Settings { get; }

        private IAnimeAggregateService AnimeAggregate { get; }

        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set { Set(() => SelectedIndex, ref _selectedIndex, value); }
        }

        public RelayCommand SubmitCommand { get; set; }

        // 

        private async void DoAction()
        {
            MessengerInstance.Send(new WorkMessage {Working = true});
            // Mark fully watched as completed
            if (SelectedIndex == 1)
            {
                var names = new List<string>();
                foreach (var anime in AnimeAggregate.AnimeService.FullyWatched())
                {
                    anime.Status = Status.Finished;
                    anime.Airing = false;
                    names.Add(anime.Title);
                }

                Settings.Save();
                var result = names.Count > 0 ? string.Join(", ", names) : "no shows";
                Methods.Alert($"Marked {result} as finished. ");
            }

            // Move duplicates to My Videos
            else if (SelectedIndex == 2)
            {
                var moveCount = await AnimeAggregate.FileService.MoveDuplicatesAsync();
                Methods.Alert($"Moved {moveCount} files to duplicate folder.");
            }

            // Regather shows with no ep. total
            else if (SelectedIndex == 3)
            {
                var updated = new List<string>();

                var needsUpdating = AnimeAggregate.AnimeService
                    .AiringAndWatching
                    .Where(a => a.MyAnimeList.HasId && (a.MyAnimeList.TotalEpisodes == 0))
                    .ToList();

                foreach (var anime in needsUpdating)
                {
                    var results = await AnimeAggregate.MalService.Find(HttpUtility.UrlEncode(anime.Title));
                    var closest = results.FirstOrDefault(r => r.Id.Equals(anime.MyAnimeList.Id));
                    if (closest != null && !anime.MyAnimeList.TotalEpisodes.Equals(closest.TotalEpisodes))
                    {
                        updated.Add(anime.Title);
                        anime.MyAnimeList.TotalEpisodes = closest.TotalEpisodes;
                    }
                }

                if (updated.Count > 0)
                {
                    var updateResult = string.Join(", ", updated);
                    Methods.Alert($"Updated total episodes for {updateResult}.");
                }

                else
                    Methods.Alert($"No shows were updated for an attempted {needsUpdating.Count} shows.");
            }
            MessengerInstance.Send(new WorkMessage { Working = false });
        }

        // 
    }
}