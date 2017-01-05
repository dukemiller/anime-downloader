using anime_downloader.Enums;
using anime_downloader.Services.Interfaces;
using anime_downloader.ViewModels.Components;
using GalaSoft.MvvmLight;

namespace anime_downloader.ViewModels
{
    public class ManageViewModel : ViewModelBase
    {
        private ViewModelBase _unwatched;

        private ViewModelBase _watched;

        // 

        public ManageViewModel(ISettingsService settings, IAnimeAggregateService animeAggregate)
        {
            Settings = settings;
            AnimeAggregate = animeAggregate;

            Unwatched = new FileListViewModel(AnimeAggregate.FileService, animeAggregate.AnimeService,
                animeAggregate.PlaylistService)
            {
                Title = "Unwatched",
                ImageResourcePath = "../Resources/Images/right.png",
                EpisodeType = EpisodeStatus.Unwatched,
                StartPath = Settings.PathConfig.Unwatched,
                MovePath = Settings.PathConfig.Watched
            };

            Watched = new FileListViewModel(AnimeAggregate.FileService, animeAggregate.AnimeService,
                animeAggregate.PlaylistService)
            {
                Title = "Watched",
                ImageResourcePath = "../Resources/Images/left.png",
                EpisodeType = EpisodeStatus.Watched,
                StartPath = Settings.PathConfig.Watched,
                MovePath = Settings.PathConfig.Unwatched,
                HideLabel = true
            };
        }

        // 

        private ISettingsService Settings { get; }

        private IAnimeAggregateService AnimeAggregate { get; }

        public ViewModelBase Unwatched
        {
            get { return _unwatched; }
            set { Set(() => Unwatched, ref _unwatched, value); }
        }

        public ViewModelBase Watched
        {
            get { return _watched; }
            set { Set(() => Watched, ref _watched, value); }
        }
    }
}