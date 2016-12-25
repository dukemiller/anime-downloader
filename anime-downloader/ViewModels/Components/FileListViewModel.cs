using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Services;
using anime_downloader.Services.Interfaces;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace anime_downloader.ViewModels.Components
{
    public class FileListViewModel : ViewModelBase
    {
        private EpisodeStatus _episodeType;
        private ObservableCollection<AnimeFile> _files;
        private string _filter;
        private ObservableCollection<AnimeFile> _filteredFiles;
        private bool _hideLabel;
        private string _imageResourcePath;
        private string _movePath;
        private string _startPath;
        private string _title;
        private AnimeFile _selectedFile;

        // 

        public FileListViewModel(IAnimeFileService fileService, IAnimeService animeService,
            IPlaylistService playlistService)
        {
            FileService = fileService;
            AnimeService = animeService;
            PlaylistService = playlistService;

            // 

            MoveCommand = new RelayCommand(Move);
            DeleteCommand = new RelayCommand(Delete);
            ProfileCommand = new RelayCommand(Profile);
            OpenCommand = new RelayCommand(Open);
            ClearFilterCommand = new RelayCommand(() => Filter = "");
            FolderCommand = new RelayCommand(() => Process.Start(StartPath));
        }

        // 

        private IAnimeFileService FileService { get; }

        private IAnimeService AnimeService { get; }

        private IPlaylistService PlaylistService { get; }

        private FileSystemWatcher Watcher { get; set; }

        private ObservableCollection<AnimeFile> Files
        {
            get { return _files; }
            set
            {
                Set(() => Files, ref _files, value);
                RaisePropertyChanged(nameof(Label));
            }
        }

        public bool HideLabel
        {
            get { return _hideLabel; }
            set { Set(() => HideLabel, ref _hideLabel, value); }
        }

        public ObservableCollection<AnimeFile> FilteredFiles
        {
            get { return _filteredFiles; }
            set { Set(() => FilteredFiles, ref _filteredFiles, value); }
        }

        public AnimeFile SelectedFile
        {
            get { return _selectedFile; }
            set { Set(() => SelectedFile, ref _selectedFile, value); }
        }

        public ObservableCollection<AnimeFile> SelectedFiles { get; set; } = new ObservableCollection<AnimeFile>();

        public EpisodeStatus EpisodeType
        {
            get { return _episodeType; }
            set
            {
                Set(() => EpisodeType, ref _episodeType, value);
                Files = new ObservableCollection<AnimeFile>(FileService.GetEpisodes(value));
                Files.CollectionChanged += (sender, args) => { RaisePropertyChanged(Label); };
                FilteredFiles = Files;
            }
        }

        public string Label => $"({Files?.Count ?? 0} files)";

        public string MovePath
        {
            get { return _movePath; }
            set { Set(() => MovePath, ref _movePath, value); }
        }

        public string StartPath
        {
            get { return _startPath; }
            set
            {
                Set(() => StartPath, ref _startPath, value);

                if (Directory.Exists(StartPath))
                {
                    Watcher = new FileSystemWatcher
                    {
                        Path = StartPath,
                        IncludeSubdirectories = true,
                        Filter = "*.*",
                        EnableRaisingEvents = true
                    };

                    Watcher.Deleted += (sender, args) =>
                    {
                        Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            var file = Files.First(f => f.Path.Equals(args.FullPath));
                            Files.Remove(file);
                            if (FilteredFiles.Contains(file))
                                FilteredFiles.Remove(file);
                            RaisePropertyChanged(nameof(Label));
                        });
                    };

                    Watcher.Created += (sender, args) =>
                    {
                        Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            var file = new AnimeFile(args.FullPath);
                            Files.AddSorted(file);
                            FilteredFiles =
                                new ObservableCollection<AnimeFile>(
                                    Files.Where(
                                        af => Methods.Strip(af.Name).ToLower().Contains(Methods.Strip(Filter).ToLower())));
                            RaisePropertyChanged(nameof(Label));
                        });
                    };
                }
            }
        }

        public string Title
        {
            get { return _title; }
            set { Set(() => Title, ref _title, value); }
        }

        public string Filter
        {
            get { return _filter; }
            set
            {
                Set(() => Filter, ref _filter, value);
                if (Filter?.Length == 0)
                    FilteredFiles = Files;
                else
                    FilteredFiles =
                        new ObservableCollection<AnimeFile>(
                            Files.Where(af => Methods.Strip(af.Name).ToLower().Contains(Methods.Strip(Filter).ToLower())));
            }
        }

        public string ImageResourcePath
        {
            get { return _imageResourcePath; }
            set { Set(() => ImageResourcePath, ref _imageResourcePath, value); }
        }

        // 

        public RelayCommand FolderCommand { get; set; }

        public RelayCommand MoveCommand { get; set; }

        public RelayCommand ProfileCommand { get; set; }

        public RelayCommand OpenCommand { get; set; }

        public RelayCommand DeleteCommand { get; set; }

        public RelayCommand ClearFilterCommand { get; set; }

        // 

        private void Move()
        {
            if ((SelectedFiles.Count > 0) && Directory.Exists(MovePath))
                foreach (var file in SelectedFiles)
                {
                    var relative = string.Join(Path.DirectorySeparatorChar.ToString(),
                        file.Path.Split(Path.DirectorySeparatorChar)
                            .Skip(StartPath.Split(Path.DirectorySeparatorChar).Length));
                    var newPath = Path.Combine(MovePath, relative);
                    Directory.Move(file.Path, newPath);
                }
        }

        private void Open()
        {
            if (SelectedFiles.Count > 1)
            {
                PlaylistService.Set(SelectedFiles);
                PlaylistService.Create();
                Process.Start(PlaylistService.Path);
            }

            else if (SelectedFile != null)
                Process.Start(SelectedFile.Path);
        }
        
        private void Profile()
        {
            if (SelectedFile != null)
            {
                var anime = FileService.ClosestAnime(AnimeService.Animes, SelectedFile);
                if (anime != null)
                {
                    MessengerInstance.Send(Enums.Views.AnimeDisplay);
                    MessengerInstance.Send(anime);
                }
                else
                    Methods.Alert($"No anime profile found for {SelectedFile.Name}.");
            }
        }

        private void Delete()
        {
            if (SelectedFiles.Count > 0)
            {
                var response = MessageBox.Show(
                    $"Files to be deleted: \n\n{string.Join("\n", SelectedFiles.Select(ep => ep.Path))}\n\n" +
                    "Are you sure?",
                    "Confirmation",
                    MessageBoxButton.YesNo);

                if (response == MessageBoxResult.Yes)
                    foreach (var episode in SelectedFiles)
                    {
                        Files.Remove(episode);
                        File.Delete(episode.Path);
                    }
            }
        }
    }
}