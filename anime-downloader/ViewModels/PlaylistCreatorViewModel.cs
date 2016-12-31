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

        // 

        public PlaylistCreatorViewModel(ISettingsService settings, IPlaylistService playlist)
        {
            Settings = settings;
            Playlist = playlist;

            // 

            Options = new ObservableCollection<RadioModel> {Default, Episode, Date};
            FileExists = File.Exists(Settings.PathConfig.Playlist);
            SelectedRadio = Options.Skip(1).First();

            // 

            CreateCommand = new RelayCommand(Create);
            OpenCommand = new RelayCommand(Open, () => FileExists);
        }

        public ISettingsService Settings { get; }

        public IPlaylistService Playlist { get; }

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
                Process.Start(Settings.PathConfig.Playlist);
        }

        private async void Create()
        {
            if (Settings.CrucialDirectoriesExist())
            {
                Playlist.Refresh();

                if (Playlist.Length == 0)
                {
                    Methods.Alert("No playlist created (no files were found in the episode folders).");
                }
                else
                {
                    if (SelectedRadio.Tag.Equals("Episode"))
                        Playlist.OrderByEpisodeNumber();
                    else if (SelectedRadio.Tag.Equals("Date"))
                        Playlist.OrderByDate();
                    // else it's just default

                    if (SeparateShowOrder)
                        Playlist.SeparateShowOrder();
                    if (ReverseOrder)
                        Playlist.ReverseOrder();

                    await Playlist.Create();

                    Methods.Alert("Playlist created.");
                }

                FileExists = File.Exists(Settings.PathConfig.Playlist);
            }
        }
    }
}