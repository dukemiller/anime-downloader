﻿using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Models.Configurations;
using anime_downloader.Repositories.Interface;
using anime_downloader.Services.Interfaces;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace anime_downloader.ViewModels.Displays
{
    public class PlaylistViewModel : ViewModelBase
    {
        private static readonly RadioModel<PlaylistOrder> Default = new RadioModel<PlaylistOrder>
        {
            Header = "Default file listing",
            ToolTip = "Default windows lexical filename sort",
            Data = PlaylistOrder.Default
        };

        private static readonly RadioModel<PlaylistOrder> NameThenEpisode = new RadioModel<PlaylistOrder>
        {
            Header = "Name then Episode",
            ToolTip = "Sort by anime name instead of file name, then by episode number",
            Data = PlaylistOrder.NameThenEpisode
        };

        private static readonly RadioModel<PlaylistOrder> EpisodeThenName = new RadioModel<PlaylistOrder>
        {
            Header = "Episode then Name",
            ToolTip = "Sort starting with the episode number, then by name",
            Data = PlaylistOrder.EpisodeThenName
        };

        private static readonly RadioModel<PlaylistOrder> Date = new RadioModel<PlaylistOrder>
        {
            Header = "Date downloaded",
            ToolTip = "When the file was created",
            Data = PlaylistOrder.Date
        };

        private static readonly RadioModel<PlaylistOrder> RandomNameThenEpisode = new RadioModel<PlaylistOrder>
        {
            Header = "Random Name",
            ToolTip = "Sort by name, randomize the order, then by episode",
            Data = PlaylistOrder.RandomNameThenEpisode
        };

        private readonly IFileService _fileService;

        private readonly ISettingsRepository _settings;

        private bool _additionalEpisodesFirst;
        
        private bool _fileExists;
        
        private ObservableCollection<RadioModel<PlaylistOrder>> _options;

        private bool _reverseOrder;

        private RadioModel<PlaylistOrder> _selectedRadio;

        private bool _separateShowOrder;

        // 

        public PlaylistViewModel(ISettingsRepository settings, IFileService fileService)
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

            Options = new ObservableCollection<RadioModel<PlaylistOrder>> {Default, NameThenEpisode, EpisodeThenName, Date, RandomNameThenEpisode };
            FileExists = File.Exists(PathConfiguration.Playlist);
            SelectedRadio = Options.Skip(1).First();
        }

        // 

        public ObservableCollection<RadioModel<PlaylistOrder>> Options
        {
            get => _options;
            set => Set(() => Options, ref _options, value);
        }

        public RadioModel<PlaylistOrder> SelectedRadio
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
        
        private Playlist Playlist { get; }

        public Playlist DemoPlaylist { get; }

        public RelayCommand OpenCommand { get; set; }

        public RelayCommand CreateCommand { get; set; }
        
        private bool FileExists
        {
            get => _fileExists;
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
            if (!await _settings.CrucialDirectoriesExist())
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