using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    public class PlaylistViewModel : ViewModelBase
    {
        private static readonly Radio<PlaylistOrder> Default = new Radio<PlaylistOrder>
        {
            Header = "Default file listing",
            ToolTip = "Default windows lexical filename sort",
            Data = PlaylistOrder.Default
        };

        private static readonly Radio<PlaylistOrder> NameThenEpisode = new Radio<PlaylistOrder>
        {
            Header = "Name then Episode",
            ToolTip = "Sort by anime name instead of file name, then by episode number",
            Data = PlaylistOrder.NameThenEpisode
        };

        private static readonly Radio<PlaylistOrder> EpisodeThenName = new Radio<PlaylistOrder>
        {
            Header = "Episode then Name",
            ToolTip = "Sort starting with the episode number, then by name",
            Data = PlaylistOrder.EpisodeThenName
        };

        private static readonly Radio<PlaylistOrder> Date = new Radio<PlaylistOrder>
        {
            Header = "Date downloaded",
            ToolTip = "When the file was created",
            Data = PlaylistOrder.Date
        };

        private static readonly Radio<PlaylistOrder> RandomNameThenEpisode = new Radio<PlaylistOrder>
        {
            Header = "Random Name",
            ToolTip = "Sort by name, randomize the order, then by episode",
            Data = PlaylistOrder.RandomNameThenEpisode
        };

        private readonly IFileService _fileService;

        private readonly ISettingsRepository _settings;

        private bool _additionalEpisodesFirst;
        
        private bool _reverseOrder;

        private Radio<PlaylistOrder> _selectedRadio;

        private bool _separateShowOrder;

        // 

        public PlaylistViewModel(ISettingsRepository settings, IFileService fileService)
        {
            _settings = settings;
            _fileService = fileService;

            DemoPlaylist = new Playlist
            {
                Source = new List<AnimeFile>
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
            
            SelectedRadio = Options.Skip(1).First();
        }

        // 

        public List<Radio<PlaylistOrder>> Options { get; set; } = Methods.List.Of(Default, NameThenEpisode,
            EpisodeThenName, Date, RandomNameThenEpisode);

        public Radio<PlaylistOrder> SelectedRadio
        {
            get => _selectedRadio;
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
            get => _separateShowOrder;
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
            get => _reverseOrder;
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
            get => _additionalEpisodesFirst;
            set
            {
                Set(() => AdditionalEpisodesFirst, ref _additionalEpisodesFirst, value);
                Playlist.Options ^= PlaylistOptions.AdditionalEpisodesFirst;
                DemoPlaylist.Options ^= PlaylistOptions.AdditionalEpisodesFirst;
                DemoPlaylist.ApplyConfiguration();
            }
        }

        public bool FileExists { get; set; } =
            File.Exists(App.Path.Playlist)
            && File.ReadAllLines(App.Path.Playlist).Any(File.Exists);

        public Playlist Playlist { get; } = new Playlist();

        public Playlist DemoPlaylist { get; }

        [DependsOn(nameof(FileExists))]
        public RelayCommand MoveCommand => new RelayCommand(Move, () => FileExists);

        [DependsOn(nameof(FileExists))]
        public RelayCommand OpenCommand => new RelayCommand(Open, () => FileExists);

        public RelayCommand CreateCommand => new RelayCommand(Create);

        public RelayCommand RadioCommand => new RelayCommand(RefreshDemo);

        // 

        private void Open()
        {
            if (FileExists)
                Process.Start(App.Path.Playlist);
        }

        private async void Create()
        {
            if (!await _settings.CrucialDirectoriesExist())
                return;

            Playlist.Source = new List<AnimeFile>(await _fileService.GetEpisodesAsync(EpisodeStatus.Unwatched));
            Playlist.ApplyConfiguration();

            if (Playlist.Source.Count == 0)
                Methods.Alert("No playlist created (no files were found in the episode folders).");

            else
            {
                FileExists = await Playlist.Create();
                Methods.Alert("Playlist created.");
            }

        }

        private void RefreshDemo()
        {
            if (SelectedRadio?.Data != PlaylistOrder.RandomNameThenEpisode)
                return;

            DemoPlaylist.ApplyConfiguration();
        }

        private void Move()
        {
            var files = File.ReadAllLines(App.Path.Playlist)
                .Where(p => p.Length > 0 && p.Contains(_settings.PathConfig.Unwatched))
                .Select(p => new AnimeFile(p))
                .ToList();

            foreach (var file in files)
                Methods.MoveFile(file,
                    _settings.PathConfig.Unwatched,
                    _settings.PathConfig.Watched);

            Methods.Alert(files.Count > 0
                ? $"Moved {files.Count} files to the watched directory."
                : "No files were moved.");

            // if we moved everything, there's no need for a playlist
            File.Delete(App.Path.Playlist);
            FileExists = false;
        }
    }
}