using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Services.Interfaces;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace anime_downloader.ViewModels
{
    public class MiscViewModel : ViewModelBase
    {
        private int _selectedIndex;

        private readonly ISettingsService _settings;
        private readonly IAnimeService _animeService;
        private readonly IMyAnimeListService _malSevice;
        private readonly IFileService _fileService;

        public MiscViewModel(ISettingsService settings, IAnimeService animeService, 
                             IMyAnimeListService malSevice, IFileService fileService)
        {
            _settings = settings;
            _animeService = animeService;
            _malSevice = malSevice;
            _fileService = fileService;
            SelectedIndex = 0;
            SubmitCommand = new RelayCommand(DoAction);
        }

        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set { Set(() => SelectedIndex, ref _selectedIndex, value); }
        }

        public RelayCommand SubmitCommand { get; set; }

        public bool LoggedIntoMal => _settings.MyAnimeListConfig.LoggedIn;

        // 

        private async void DoAction()
        {
            MessengerInstance.Send(new WorkMessage {Working = true});

            // Mark fully watched as completed
            if (SelectedIndex == 1)
            {
                var names = new List<string>();
                foreach (var anime in _animeService.FullyWatched())
                {
                    anime.Status = Status.Finished;
                    anime.Airing = false;
                    names.Add(anime.Title);
                }

                _settings.Save();
                var result = names.Count > 0 ? string.Join(", ", names) : "no shows";
                Methods.Alert($"Marked {result} as finished. ");
            }

            // Move duplicates to My Videos
            else if (SelectedIndex == 2)
            {
                var moveCount = await _fileService.MoveDuplicatesAsync();
                Methods.Alert($"Moved {moveCount} files to duplicate folder.");
            }

            // Regather shows with no ep. total
            else if (SelectedIndex == 3)
            {
                var updated = new List<string>();

                var needsUpdating = _animeService
                    .AiringAndWatching
                    .Where(a => a.MyAnimeList.HasId && a.MyAnimeList.TotalEpisodes == 0)
                    .ToList();

                foreach (var anime in needsUpdating)
                {
                    var remoteAnime = await _malSevice.GetFindResult(anime);
                    if (!anime.MyAnimeList.TotalEpisodes.Equals(remoteAnime.TotalEpisodes))
                    {
                        updated.Add(anime.Title);
                        anime.MyAnimeList.TotalEpisodes = remoteAnime.TotalEpisodes;
                    }
                }

                if (updated.Count > 0)
                {
                    var updateResult = string.Join(", ", updated);
                    Methods.Alert($"Updated total episodes for {updateResult}.");
                }

                else
                {
                    Methods.Alert($"No shows were updated for an attempted {needsUpdating.Count} shows.");
                }
            }

            // Set current episode to last found file's episode number
            else if (SelectedIndex == 4)
            {
                var changed = new List<string>();

                await Task.Run(() =>
                {
                    foreach (var anime in _animeService.AiringAndWatching)
                    {
                        var lastEpisode = _fileService.LastEpisode(anime);
                        if (lastEpisode != null && anime.Episode != lastEpisode.Episode)
                        {
                            anime.Episode = lastEpisode.Episode;
                            changed.Add(anime.Title);
                        }
                    }
                });

                if (changed.Count > 0)
                    Methods.Alert($"Updated episodes for: {string.Join(", ", changed)}");
                else
                    Methods.Alert("No re-indexes were needed.");
            }
            
            MessengerInstance.Send(new WorkMessage {Working = false});
        }

        // 
    }
}