using System.Threading.Tasks;
using anime_downloader.Enums;
using anime_downloader.Repositories.Interface;
using anime_downloader.Services.Interfaces;
using anime_downloader.ViewModels.Components;
using GalaSoft.MvvmLight;

namespace anime_downloader.ViewModels.Displays
{
    public class ManageViewModel : ViewModelBase
    {
        private readonly ISettingsRepository _settings;
        private readonly IFileService _fileService;
        private readonly IAnimeService _animeService;
        
        // 

        public ManageViewModel(ISettingsRepository settings, IFileService fileService, IAnimeService animeService)
        {
            _settings = settings;
            _fileService = fileService;
            _animeService = animeService;
            _settings.PathConfig.PropertyChanged += (sender, args) => Load();
            Load();
        }

        // 
        
        public FileListViewModel Unwatched { get; set; }

        public FileListViewModel Watched { get; set; }

        //

        private async void Load()
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