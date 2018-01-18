using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Models.Configurations;
using anime_downloader.Repositories.Interface;
using anime_downloader.Services.Interfaces;
using anime_downloader.ViewModels.Components;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace anime_downloader.ViewModels.Displays
{
    public class MiscViewModel : ViewModelBase
    {
        private int _selectedIndex;

        private readonly ICredentialsRepository _credentialsRepository;
        private readonly IAnimeRepository _animeRepository;
        private readonly IAnimeService _animeService;
        private readonly IDetailProviderService _detailService;
        private readonly IFileService _fileService;
        private readonly ISettingsRepository _settingsRepository;
        private bool _doingAction;

        public MiscViewModel(ICredentialsRepository credentialsRepository, 
            IAnimeRepository animeRepository,
            IAnimeService animeService,
            IDetailProviderService detailService,
            IFileService fileService,
            ISettingsRepository settingsRepository)
        {
            _credentialsRepository = credentialsRepository;
            _animeRepository = animeRepository;
            _animeService = animeService;
            _detailService = detailService;
            _fileService = fileService;
            _settingsRepository = settingsRepository;
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
            MessengerInstance.Send(ViewState.IsWorking);
            DoingAction = true;

            // Mark fully watched as completed
            if (SelectedIndex == 1)
            {
                var updated = new List<string>();
                foreach (var anime in _animeService.FullyWatched())
                {
                    anime.Status = Status.Finished;
                    anime.Airing = false;
                    updated.Add("-- " + anime.Title);
                }

                _animeRepository.Save();
                var result = updated.Count > 0 ? string.Join("\n", updated) : "no shows";
                Methods.Alert(updated.Count > 0 ? $"Shows marked as finished:\n{result}" : "No shows were updated.");
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
                            updated.Add("-- " + anime.Title);
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
                            changed.Add("-- " + anime.Title);
                        }
                    }
                });

                Methods.Alert(changed.Count > 0
                    ? $"Updated episodes for:\n{string.Join("\n", changed)}"
                    : "No re-indexes were needed.");

                if (changed.Count > 0)
                    _animeRepository.Save();

                // Update animelist
                MessengerInstance.Send(ViewRequest.Update);
            }

            // Move all files on playlist to Watched
            else if (SelectedIndex == 5)
            {
                var files = File.ReadAllLines(PathConfiguration.Playlist)
                    .Where(p => p.Length > 0 && p.Contains(_settingsRepository.PathConfig.Unwatched))
                    .Select(p => new AnimeFile(p))
                    .ToList();

                foreach (var file in files)
                    Methods.MoveFile(file, 
                        _settingsRepository.PathConfig.Unwatched,
                        _settingsRepository.PathConfig.Watched);

                Methods.Alert(files.Count > 0
                    ? $"Moved {files.Count} files to the watched directory."
                    : "No files were moved.");
            }

            MessengerInstance.Send(ViewState.DoneWorking);
            DoingAction = false;
        }

        // 
    }
}