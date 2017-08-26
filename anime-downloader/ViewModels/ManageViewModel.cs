using System.Threading.Tasks;
using anime_downloader.Enums;
using anime_downloader.Services.Interfaces;
using anime_downloader.ViewModels.Components;
using GalaSoft.MvvmLight;

namespace anime_downloader.ViewModels
{
    public class ManageViewModel : ViewModelBase
    {
        private readonly ISettingsService _settings;
        private readonly IFileService _fileService;
        private readonly IAnimeService _animeService;
        private FileListViewModel _unwatched;
        private FileListViewModel _watched;
        
        // 

        public ManageViewModel(ISettingsService settings, IFileService fileService, IAnimeService animeService)
        {
            _settings = settings;
            _fileService = fileService;
            _animeService = animeService;
            _settings.PathConfig.PropertyChanged += (sender, args) => LoadFolders();
            LoadFolders();
        }

        // 
        
        public FileListViewModel Unwatched
        {
            get => _unwatched;
            set => Set(() => Unwatched, ref _unwatched, value);
        }

        public FileListViewModel Watched
        {
            get => _watched;
            set => Set(() => Watched, ref _watched, value);
        }

        //

        private async void LoadFolders()
        {
            await Task.Run(() =>
            {
                Unwatched = new FileListViewModel(_fileService, _animeService)
                {
                    Title = "Unwatched",
                    ImageResourcePath = "../Resources/Images/right.png",
                    EpisodeType = EpisodeStatus.Unwatched,
                    StartPath = _settings.PathConfig.Unwatched,
                    MovePath = _settings.PathConfig.Watched
                };

                Watched = new FileListViewModel(_fileService, _animeService)
                {
                    Title = "Watched",
                    ImageResourcePath = "../Resources/Images/left.png",
                    EpisodeType = EpisodeStatus.Watched,
                    StartPath = _settings.PathConfig.Watched,
                    MovePath = _settings.PathConfig.Unwatched,
                    HideLabel = true
                };
            });
        }
    }
}