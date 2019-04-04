using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Repositories.Interface;
using anime_downloader.Services.Interfaces;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using PropertyChanged;

namespace anime_downloader.ViewModels.Displays
{
    public class MiscViewModel : ViewModelBase
    {
        private readonly ICredentialsRepository _credentialsRepository;
        private readonly IAnimeRepository _animeRepository;
        private readonly IAnimeService _animeService;
        private readonly IDetailProviderService _detailService;
        private readonly IFileService _fileService;

        public MiscViewModel(ICredentialsRepository credentialsRepository, 
            IAnimeRepository animeRepository,
            IAnimeService animeService,
            IDetailProviderService detailService,
            IFileService fileService)
        {
            _credentialsRepository = credentialsRepository;
            _animeRepository = animeRepository;
            _animeService = animeService;
            _detailService = detailService;
            _fileService = fileService;
        }

        public int SelectedIndex { get; set; } = 0;

        [DependsOn(nameof(DoingAction))]
        public RelayCommand SubmitCommand => new RelayCommand(DoAction, () => !DoingAction);

        public bool DoingAction { get; set; }

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
                foreach (var anime in _animeService.FullyWatched)
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
                        var (_, changeMade) = await _detailService.FillInDetails(anime);
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
                        _fileService
                            .LastEpisode(anime)
                            .Filter(lastEpisode => anime.Episode != lastEpisode.Episode)
                            .MatchSome(lastEpisode =>
                            {
                                anime.Episode = lastEpisode.Episode;
                                changed.Add("-- " + anime.Title);
                            });
                });

                Methods.Alert(changed.Count > 0
                    ? $"Updated episodes for:\n{string.Join("\n", changed)}"
                    : "No re-indexes were needed.");

                if (changed.Count > 0)
                    _animeRepository.Save();

                // Update animelist
                MessengerInstance.Send(ViewRequest.Update);
            }

            MessengerInstance.Send(ViewState.DoneWorking);
            DoingAction = false;
        }

    }
}