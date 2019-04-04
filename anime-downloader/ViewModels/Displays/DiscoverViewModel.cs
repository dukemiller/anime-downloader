using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Models.AniList;
using anime_downloader.Services.Interfaces;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Optional;
using static anime_downloader.Classes.Methods;

namespace anime_downloader.ViewModels.Displays
{
    public class DiscoverViewModel : ViewModelBase
    {
        private readonly IFindSeasonAnimeService _findService;
        private readonly IAnimeService _animeService;
        private bool _visible;
        private int? _previousIndex;
        private List<AiringAnime> _airing;
        private List<AiringAnime> _leftover;

        // 

        public DiscoverViewModel(IFindSeasonAnimeService findService, IAnimeService animeService)
        {
            _findService = findService;
            _animeService = animeService;

            MessengerInstance.Register<ViewRequest>(this, HandleViewAction);

            PropertyChanged += async (sender, args) =>
            {
                if (args.PropertyName == "SelectedAiring")
                    await _findService.CollectResources(SelectedAiring);
                if (args.PropertyName == "SelectedLeftover")
                    await _findService.CollectResources(SelectedLeftover);
            };
        }

        // 

        public AiringAnime SelectedAiring { get; set; }

        public AiringAnime SelectedLeftover { get; set; }

        public AnimeSeason Season { get; set; } = AnimeSeason.Current;

        public int SelectedIndex { get; set; }

        public List<AiringAnime> AiringShows { get; set; }

        public List<AiringAnime> LeftoverShows { get; set; }

        public RelayCommand AddCommand => new RelayCommand(Add);

        // 

        private void OnSeasonChanged()
        {
            AiringShows.Clear();
            LeftoverShows.Clear();
            LoadPage();
        }

        private void OnSelectedIndexChanged() => LoadPage();

        /// <summary>
        ///     Only attempt to load properties from the view when it is visible.
        /// </summary>
        public void VisibilityChanged(bool visible)
        {
            _visible = visible;

            if (_visible)
                LoadPage();
        }

        private void HandleViewAction(ViewRequest va)
        {
            if (va == ViewRequest.Refresh)
                Refresh();
        }

        private void Refresh()
        {
            if (_airing != null)
                AiringShows = _airing.Where(Not<AiringAnime>(_animeService.WatchingAndAiringContains)).ToList();
            if (_leftover != null)
                LeftoverShows = _leftover.Where(Not<AiringAnime>(_animeService.WatchingAndAiringContains)).ToList();

            switch (SelectedIndex)
            {
                case 0:
                    if (AiringShows != null)
                        SelectedAiring = _previousIndex != null && _previousIndex.Value > 0 &&
                                         _previousIndex.Value < AiringShows.Count && AiringShows.Count > 0
                            ? AiringShows[_previousIndex.Value - 1]
                            : AiringShows.FirstOrDefault();
                    break;
                case 1:
                    if (LeftoverShows != null)
                        SelectedLeftover = _previousIndex != null && _previousIndex.Value > 0 &&
                                           _previousIndex.Value < LeftoverShows.Count && LeftoverShows.Count > 0
                            ? LeftoverShows[_previousIndex.Value - 1]
                            : LeftoverShows.FirstOrDefault();
                    break;
                default:
                    SelectedAiring = null;
                    SelectedLeftover = null;
                    break;
            }
        }

        private async void LoadPage()
        {
            switch (SelectedIndex)
            {
                // First tab: Airing Shows
                case 0:
                    if (AiringShows is null)
                    {
                        _airing = await _findService.New(Season, () => MessengerInstance.Send(ViewState.IsLoading));
                        await Task.Run(() => AiringShows = _airing.Where(Not<AiringAnime>(_animeService.WatchingAndAiringContains)).ToList());
                        MessengerInstance.Send(ViewState.DoneLoading);
                    }
                    break;

                // Second tab: Leftover
                case 1:
                    if (LeftoverShows is null)
                    {
                        _leftover = await _findService.Leftover(Season, () => MessengerInstance.Send(ViewState.IsLoading));
                        await Task.Run(() => LeftoverShows = _leftover.Where(Not<AiringAnime>(_animeService.WatchingAndAiringContains)).ToList());
                        MessengerInstance.Send(ViewState.DoneLoading);
                    }
                    break;
                default:
                    break;
            }
        }

        private void Add() => (SelectedIndex == 0 ? SelectedAiring : SelectedLeftover).SomeNotNull().MatchSome(anime =>
        {
            MessengerInstance.Send(Display.Anime);
            MessengerInstance.Send(anime);
            _previousIndex = SelectedIndex == 0 ? AiringShows.IndexOf(anime) : LeftoverShows.IndexOf(anime);
        });
    }
}