using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using anime_downloader.Classes;
using anime_downloader.Models;
using anime_downloader.Services.Interfaces;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace anime_downloader.ViewModels
{
    public class PlaylistCreatorViewModel : ViewModelBase
    {
        private static readonly RadioModel Default = new RadioModel
        {
            Header = "Default file listing",
            ToolTip = "Default windows lexical filename sort",
            Tag = "Default"
        };

        private static readonly RadioModel Episode = new RadioModel
        {
            Header = "Episode number",
            ToolTip = "A more rigorous episode number check so that episodes are in order",
            Tag = "Episode"
        };

        private static readonly RadioModel Date = new RadioModel
        {
            Header = "Date downloaded",
            ToolTip = "When the file was created",
            Tag = "Date"
        };

        private bool _fileExists;

        private ObservableCollection<RadioModel> _options;

        private bool _reverseOrder;

        private RadioModel _selectedRadio;

        private bool _separateShowOrder;

        private bool _additionalEpisodesFirst;

        // 

        public PlaylistCreatorViewModel(ISettingsService settings, IPlaylistService playlist)
        {
            _settings = settings;
            _playlist = playlist;

            // 

            Options = new ObservableCollection<RadioModel> {Default, Episode, Date};
            FileExists = File.Exists(_settings.PathConfig.Playlist);
            SelectedRadio = Options.Skip(1).First();

            // 

            CreateCommand = new RelayCommand(Create);
            OpenCommand = new RelayCommand(Open, () => FileExists);
        }

        private readonly ISettingsService _settings;

        private readonly IPlaylistService _playlist;

        // 

        public bool SeparateShowOrder
        {
            get { return _separateShowOrder; }
            set { Set(() => SeparateShowOrder, ref _separateShowOrder, value); }
        }

        public bool ReverseOrder
        {
            get { return _reverseOrder; }
            set { Set(() => ReverseOrder, ref _reverseOrder, value); }
        }

        public bool AdditionalEpisodesFirst
        {
            get { return _additionalEpisodesFirst; }
            set { Set(() => AdditionalEpisodesFirst, ref _additionalEpisodesFirst, value); }
        }

        public ObservableCollection<RadioModel> Options
        {
            get { return _options; }
            set { Set(() => Options, ref _options, value); }
        }

        public RadioModel SelectedRadio
        {
            get { return _selectedRadio; }
            set { Set(() => SelectedRadio, ref _selectedRadio, value); }
        }

        public RelayCommand OpenCommand { get; set; }

        public RelayCommand CreateCommand { get; set; }

        public bool FileExists
        {
            get { return _fileExists; }
            set { Set(() => FileExists, ref _fileExists, value); }
        }

        // 

        private void Open()
        {
            if (FileExists)
                Process.Start(_settings.PathConfig.Playlist);
        }

        private async void Create()
        {
            if (_settings.CrucialDirectoriesExist())
            {
                _playlist.Refresh();

                if (_playlist.Length == 0)
                {
                    Methods.Alert("No playlist created (no files were found in the episode folders).");
                }

                else
                {
                    if (SelectedRadio.Tag.Equals("Episode"))
                        _playlist.OrderByEpisodeNumber();
                    else if (SelectedRadio.Tag.Equals("Date"))
                        _playlist.OrderByDate();
                    // else it's just default

                    if (SeparateShowOrder)
                        _playlist.SeparateShowOrder();
                    if (ReverseOrder)
                        _playlist.ReverseOrder();
                    if (AdditionalEpisodesFirst)
                        _playlist.AdditionalEpisodesFirst();

                    await _playlist.Create();

                    Methods.Alert("Playlist created.");
                }

                FileExists = File.Exists(_settings.PathConfig.Playlist);
            }
        }
    }
}