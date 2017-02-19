using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Models.Configurations;
using anime_downloader.Services.Interfaces;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace anime_downloader.ViewModels
{
    public class PlaylistCreatorViewModel : ViewModelBase
    {
        private static readonly RadioModel<PlaylistOrder> Default = new RadioModel<PlaylistOrder>
        {
            Header = "Default file listing",
            ToolTip = "Default windows lexical filename sort",
            Tag = "Default",
            Data = PlaylistOrder.Default
        };

        private static readonly RadioModel<PlaylistOrder> NameThenEpisode = new RadioModel<PlaylistOrder>
        {
            Header = "Name then Episode",
            ToolTip = "Sort by anime name instead of file name, then by episode number",
            Tag = "Episode",
            Data = PlaylistOrder.NameThenEpisode
        };

        private static readonly RadioModel<PlaylistOrder> EpisodeThenName = new RadioModel<PlaylistOrder>
        {
            Header = "Episode then Name",
            ToolTip = "Sort starting with the episode number, then by name",
            Tag = "Episode",
            Data = PlaylistOrder.EpisodeThenName
        };

        private static readonly RadioModel<PlaylistOrder> Date = new RadioModel<PlaylistOrder>
        {
            Header = "Date downloaded",
            ToolTip = "When the file was created",
            Tag = "Date",
            Data = PlaylistOrder.Date
        };

        private readonly IFileService _fileService;

        private readonly ISettingsService _settings;

        private bool _additionalEpisodesFirst;
        
        private bool _fileExists;
        
        private ObservableCollection<RadioModel<PlaylistOrder>> _options;

        private bool _reverseOrder;

        private RadioModel<PlaylistOrder> _selectedRadio;

        private bool _separateShowOrder;

        // 

        public PlaylistCreatorViewModel(ISettingsService settings, IFileService fileService)
        {
            _settings = settings;
            _fileService = fileService;

            // 

            Playlist = new Playlist();
            DemoPlaylist = new Playlist
            {
                Source = new ObservableCollection<AnimeFile>
                {
                    new AnimeFile("[GoodSubs] Slice of life - 01"),
                    new AnimeFile("[GoodSubs] Slice of life - 02"),
                    new AnimeFile("[GoodSubs] Slice of life - 03"),
                    new AnimeFile("[GoodSubs] Slice of life - 04"),
                    new AnimeFile("[AbcSubs] Another slice of life - 22"),
                    new AnimeFile("[XyzSubs] Action - 08"),
                    new AnimeFile("[XyzSubs] Action - 09"),
                    new AnimeFile("[XyzSubs] Action - 10"),
                    new AnimeFile("[AbcSubs] Comedy - 125"),
                    new AnimeFile("[AbcSubs] Comedy - 126"),
                    new AnimeFile("[NoSubs] Adventure - 20")
                }
            };

            // 

            CreateCommand = new RelayCommand(Create);
            OpenCommand = new RelayCommand(Open, () => FileExists);

            // 

            Options = new ObservableCollection<RadioModel<PlaylistOrder>> {Default, NameThenEpisode, EpisodeThenName, Date};
            FileExists = File.Exists(PathConfiguration.Playlist);
            SelectedRadio = Options.Skip(1).First();
        }

        // 

        public ObservableCollection<RadioModel<PlaylistOrder>> Options
        {
            get { return _options; }
            set { Set(() => Options, ref _options, value); }
        }

        public RadioModel<PlaylistOrder> SelectedRadio
        {
            get { return _selectedRadio; }
            set
            {
                Set(() => SelectedRadio, ref _selectedRadio, value);
                Playlist.Order = SelectedRadio.Data;
                DemoPlaylist.Order = SelectedRadio.Data;
                DemoPlaylist.ApplyConfiguration();
            }
        }

        public bool SeparateShowOrder
        {
            get { return _separateShowOrder; }
            set
            {
                Set(() => SeparateShowOrder, ref _separateShowOrder, value);
                Playlist.Options ^= PlaylistOptions.SeparateShowOrder;
                DemoPlaylist.Options ^= PlaylistOptions.SeparateShowOrder;
                DemoPlaylist.ApplyConfiguration();
            }
        }

        public bool ReverseOrder
        {
            get { return _reverseOrder; }
            set
            {
                Set(() => ReverseOrder, ref _reverseOrder, value);
                Playlist.Options ^= PlaylistOptions.Reverse;
                DemoPlaylist.Options ^= PlaylistOptions.Reverse;
                DemoPlaylist.ApplyConfiguration();
            }
        }

        public bool AdditionalEpisodesFirst
        {
            get { return _additionalEpisodesFirst; }
            set
            {
                Set(() => AdditionalEpisodesFirst, ref _additionalEpisodesFirst, value);
                Playlist.Options ^= PlaylistOptions.AdditionalEpisodesFirst;
                DemoPlaylist.Options ^= PlaylistOptions.AdditionalEpisodesFirst;
                DemoPlaylist.ApplyConfiguration();
            }
        }
        
        private Playlist Playlist { get; }

        public Playlist DemoPlaylist { get; }

        public RelayCommand OpenCommand { get; set; }

        public RelayCommand CreateCommand { get; set; }
        
        private bool FileExists
        {
            get { return _fileExists; }
            set
            {
                Set(() => FileExists, ref _fileExists, value);
                OpenCommand.RaiseCanExecuteChanged();
            }
        }

        // 

        private void Open()
        {
            if (FileExists)
                Process.Start(PathConfiguration.Playlist);
        }

        private async void Create()
        {
            if (!_settings.CrucialDirectoriesExist())
                return;

            Playlist.Source = new ObservableCollection<AnimeFile>(await _fileService.GetEpisodesAsync(EpisodeStatus.Unwatched));
            Playlist.ApplyConfiguration();

            if (Playlist.Source.Count == 0)
                Methods.Alert("No playlist created (no files were found in the episode folders).");

            else
            {
                await Playlist.Create();
                Methods.Alert("Playlist created.");
            }

            FileExists = File.Exists(PathConfiguration.Playlist);
        }
    }
}