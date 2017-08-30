using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Repositories.Interface;
using anime_downloader.Services.Interfaces;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace anime_downloader.ViewModels
{
    public class MiscViewModel : ViewModelBase
    {
        private int _selectedIndex;

        private readonly ICredentialsRepository _credentialsRepository;
        private readonly IAnimeRepository _animeRepository;
        private readonly IAnimeService _animeService;
        private readonly IMyAnimeListService _malSevice;
        private readonly IDetailProviderService _detailService;
        private readonly IFileService _fileService;
        private bool _doingAction;

        public MiscViewModel(ICredentialsRepository credentialsRepository, 
            IAnimeRepository animeRepository,
            IAnimeService animeService,
            IMyAnimeListService malSevice, 
            IDetailProviderService detailService,
            IFileService fileService)
        {
            _credentialsRepository = credentialsRepository;
            _animeRepository = animeRepository;
            _animeService = animeService;
            _malSevice = malSevice;
            _detailService = detailService;
            _fileService = fileService;
            SelectedIndex = 0;
            SubmitCommand = new RelayCommand(DoAction, () => !DoingAction);
            // 
        }

        public int SelectedIndex
        {
            get => _selectedIndex;
            set => Set(() => SelectedIndex, ref _selectedIndex, value);
        }

        public RelayCommand SubmitCommand { get; set; }

        public bool DoingAction
        {
            private get => _doingAction;
            set
            {
                Set(() => DoingAction, ref _doingAction, value);
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }

        public bool LoggedIntoMal => _credentialsRepository.MyAnimeListConfig.LoggedIn;

        // 

        private async void DoAction()
        {
            MessengerInstance.Send(new WorkMessage {Working = true});
            DoingAction = true;

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

                _animeRepository.Save();
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
                    .Where(a => a.Details.TotalEpisodes == 0)
                    .ToList();

                try
                {
                    foreach (var anime in needsUpdating)
                    {
                        var (successful, changeMade) = await _detailService.FillInDetails(anime);
                        if (changeMade)
                            updated.Add(anime.Title);
                    }

                    if (updated.Count > 0)
                    {
                        _animeRepository.Save();
                        var updateResult = string.Join("\n", updated);
                        Methods.Alert($"Updated information for:\n{updateResult}.");
                    }

                    else
                    {
                        Methods.Alert($"No shows were updated for an attempted {needsUpdating.Count} shows.");
                    }
                }

                catch (HttpRequestException)
                {
                    Methods.Alert("There was a problem making a connection.");
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

                Methods.Alert(changed.Count > 0
                    ? $"Updated episodes for: {string.Join(", ", changed)}"
                    : "No re-indexes were needed.");

                if (changed.Count > 0)
                    _animeRepository.Save();

                // Update animelist
                MessengerInstance.Send("update");
            }

            MessengerInstance.Send(new WorkMessage {Working = false});
            DoingAction = false;
        }

        // 
    }
}