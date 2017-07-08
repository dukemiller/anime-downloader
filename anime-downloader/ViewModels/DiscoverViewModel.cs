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

namespace anime_downloader.ViewModels
{
    public class DiscoverViewModel : ViewModelBase
    {
        private readonly IFindSeasonAnimeService _findService;
        private readonly IAnimeService _animeService;
        private bool _visible;
        private int _selectedIndex;
        private AnimeSeason _season = AnimeSeason.Current;
        private ObservableCollection<AiringAnime> _airingShows;
        private ObservableCollection<AiringAnime> _leftoverShows;
        private AiringAnime _selectedAiring;

        // 

        public DiscoverViewModel(IFindSeasonAnimeService findService, IAnimeService animeService)
        {
            _findService = findService;
            _animeService = animeService;
            AddCommand = new RelayCommand(Add);

            MessengerInstance.Register<string>(this, _ =>
            {
                if (_ == "refresh")
                    Refresh();
            });
        }

        // 

        public AiringAnime SelectedAiring
        {
            get => _selectedAiring;
            set
            {
                Set(() => SelectedAiring, ref _selectedAiring, value);
                if (value != null && string.IsNullOrEmpty(SelectedAiring?.Description))
                    FillOutDetails();
            }
        }

        public AiringAnime SelectedLeftover
        {
            get => _selectedLeftover;
            set
            {
                Set(() => SelectedLeftover, ref _selectedLeftover, value);
                if (value != null && string.IsNullOrEmpty(SelectedLeftover?.Description))
                    FillOutDetails();
            }
        }

        public AnimeSeason Season
        {
            get => _season;
            set
            {
                Set(() => Season, ref _season, value);
                AiringShows.Clear();
                LeftoverShows.Clear();
                LoadPage();
            }
        }

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                Set(() => SelectedIndex, ref _selectedIndex, value);
                LoadPage();
            }
        }

        public ObservableCollection<AiringAnime> AiringShows
        {
            get => _airingShows;
            set => Set(() => AiringShows, ref _airingShows, value);
        }

        public ObservableCollection<AiringAnime> LeftoverShows
        {
            get => _leftoverShows;
            set => Set(() => LeftoverShows, ref _leftoverShows, value);
        }

        public RelayCommand AddCommand { get; set; }

        private List<AiringAnime> _airing;

        private List<AiringAnime> _leftover;

        private AiringAnime _selectedLeftover;

        // 

        /// <summary>
        ///     Only attempt to load properties from the view when it is visible.
        /// </summary>
        /// <param name="visible"></param>
        public void VisibilityChanged(bool visible)
        {
            _visible = visible;

            if (_visible)
                LoadPage();
        }

        private void Refresh()
        {
            if (_airing != null)
                AiringShows = new ObservableCollection<AiringAnime>(_airing
                    .Where(anime =>    !_animeService.ListContainsName(anime.TitleEnglish)
                                    && !_animeService.ListContainsName(anime.TitleRomaji)));
            if (_leftover != null)
                LeftoverShows = new ObservableCollection<AiringAnime>(_leftover
                    .Where(anime =>    !_animeService.ListContainsName(anime.TitleEnglish)
                                    && !_animeService.ListContainsName(anime.TitleRomaji)));

            switch (SelectedIndex)
            {
                case 0:
                    SelectedAiring = AiringShows.FirstOrDefault();
                    break;
                case 1:
                    SelectedAiring = LeftoverShows.FirstOrDefault();
                    break;
                default:
                    SelectedAiring = null;
                    break;
            }
        }

        private async void LoadPage()
        {
            switch (SelectedIndex)
            {
                // First tab: Airing Shows
                case 0:
                    if (AiringShows == null)
                    {
                        MessengerInstance.Send("loading");
                        _airing = await _findService.New(Season);
                        await Task.Run(() =>
                            AiringShows = new ObservableCollection<AiringAnime>(_airing
                                .Where(anime => !_animeService.ListContainsName(anime.TitleEnglish)
                                                && !_animeService.ListContainsName(anime.TitleRomaji))));
                        MessengerInstance.Send("loading");
                    }
                    break;

                // Second tab: Leftover
                case 1:
                    if (LeftoverShows == null)
                    {
                        MessengerInstance.Send("loading");
                        _leftover = await _findService.Leftover(Season);
                        await Task.Run(() =>
                            LeftoverShows = new ObservableCollection<AiringAnime>(_leftover
                                .Where(anime =>    !_animeService.ListContainsName(anime.TitleEnglish)
                                                && !_animeService.ListContainsName(anime.TitleRomaji))));
                        MessengerInstance.Send("loading");
                    }
                    break;
                default:
                    break;
            }
        }

        private async void FillOutDetails()
        {
            var anime = SelectedIndex == 0 ? SelectedAiring : SelectedLeftover;
            if (anime != null)
            {
                MessengerInstance.Send("loading");
                await _findService.FillInDetails(Season, SelectedIndex == 0, anime);
                MessengerInstance.Send("loading");
            }
        }

        private void Add()
        {
            var anime = SelectedIndex == 0 ? SelectedAiring : SelectedLeftover;
            if (anime != null)
            {
                MessengerInstance.Send(ViewDisplay.Anime);
                MessengerInstance.Send(anime);
            }
        }
    }
}