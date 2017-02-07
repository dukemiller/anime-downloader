using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Services.Interfaces;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.VisualBasic.FileIO;

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

        private AnimeFile _selectedFile;

        private string _startPath;

        private string _title;

        private readonly IFileService _fileService;

        private readonly IAnimeService _animeService;

        private readonly IPlaylistService _playlistService;

        // 

        public FileListViewModel(IFileService fileService, IAnimeService animeService,
            IPlaylistService playlistService)
        {
            _fileService = fileService;
            _animeService = animeService;
            _playlistService = playlistService;

            // 

            MoveCommand = new RelayCommand(Move);
            DeleteCommand = new RelayCommand(Delete);
            ProfileCommand = new RelayCommand(Profile);
            OpenCommand = new RelayCommand(Open);
            ClearFilterCommand = new RelayCommand(() => Filter = "");
            FolderCommand = new RelayCommand(() => Process.Start(StartPath));
            MalCommand = new RelayCommand(MyAnimeListProfile);
        }

        // Properties
        
        private FileSystemWatcher Watcher { get; set; }

        /// <summary>
        ///     The parent files that the source retrives views from
        /// </summary>
        private ObservableCollection<AnimeFile> Files
        {
            get { return _files; }
            set
            {
                Set(() => Files, ref _files, value);
                RaisePropertyChanged(nameof(Label));
            }
        }

        /// <summary>
        ///     A label simply showing the count of total files
        /// </summary>
        public string Label => $"({Files?.Count ?? 0} files)";

        /// <summary>
        ///     A user flag to note if the file count should be hidden or not
        /// </summary>
        public bool HideLabel
        {
            get { return _hideLabel; }
            set { Set(() => HideLabel, ref _hideLabel, value); }
        }

        /// <summary>
        ///     The actual views display of files
        /// </summary>
        public ObservableCollection<AnimeFile> FilteredFiles
        {
            get { return _filteredFiles; }
            set { Set(() => FilteredFiles, ref _filteredFiles, value); }
        }

        /// <summary>
        ///     Either the only selected file or the first selected of a group of files
        /// </summary>
        public AnimeFile SelectedFile
        {
            get { return _selectedFile; }
            set { Set(() => SelectedFile, ref _selectedFile, value); }
        }

        /// <summary>
        ///     All selected files
        /// </summary>
        public ObservableCollection<AnimeFile> SelectedFiles { get; set; } = new ObservableCollection<AnimeFile>();

        /// <summary>
        ///     The episode type that will determine what episodes are filled in the Files list
        /// </summary>
        public EpisodeStatus EpisodeType
        {
            get { return _episodeType; }
            set
            {
                Set(() => EpisodeType, ref _episodeType, value);
                Files = new ObservableCollection<AnimeFile>(_fileService.GetEpisodes(value));
                Files.CollectionChanged += (sender, args) => { RaisePropertyChanged(Label); };
                FilteredFiles = Files;
            }
        }
        
        /// <summary>
        ///     The target destination
        /// </summary>
        public string MovePath
        {
            private get { return _movePath; }
            set { Set(() => MovePath, ref _movePath, value); }
        }

        /// <summary>
        ///     The source destination
        /// </summary>
        public string StartPath
        {
            private get { return _startPath; }
            set
            {
                Set(() => StartPath, ref _startPath, value);

                if (!Directory.Exists(StartPath))
                    return;

                Watcher = new FileSystemWatcher
                {
                    Path = StartPath,
                    IncludeSubdirectories = true,
                    Filter = "*.*"
                };
                Watcher.Deleted += DeleteMethodLogic;
                Watcher.Renamed += RenameMethodLogic;
                Watcher.Created += CreateMethodLogic;
                Watcher.EnableRaisingEvents = true;
            }
        }
        
        /// <summary>
        ///     Display title at the top of the control
        /// </summary>
        public string Title
        {
            get { return _title; }
            set { Set(() => Title, ref _title, value); }
        }

        /// <summary>
        ///     Text that determines the FilteredFiles list subset of the Files list
        /// </summary>
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

        /// <summary>
        ///     The path for the image representing which direction the files will move
        /// </summary>
        public string ImageResourcePath
        {
            get { return _imageResourcePath; }
            set { Set(() => ImageResourcePath, ref _imageResourcePath, value); }
        }

        // Commands

        public RelayCommand FolderCommand { get; set; }

        public RelayCommand MoveCommand { get; set; }

        public RelayCommand ProfileCommand { get; set; }

        public RelayCommand MalCommand { get; set; }

        public RelayCommand OpenCommand { get; set; }

        public RelayCommand DeleteCommand { get; set; }

        public RelayCommand ClearFilterCommand { get; set; }

        // 

        /// <summary>
        ///     Move all selected files from from {StartPath} to {MovePath}
        /// </summary>
        private void Move()
        {
            if (SelectedFiles.Count > 0 && Directory.Exists(MovePath))
                foreach (var file in SelectedFiles)
                {
                    var relative = string.Join(Path.DirectorySeparatorChar.ToString(),
                        file.Path.Split(Path.DirectorySeparatorChar)
                            .Skip(StartPath.Split(Path.DirectorySeparatorChar).Length));
                    var newPath = Path.Combine(MovePath, relative);
                    Directory.Move(file.Path, newPath);
                }
        }

        /// <summary>
        ///     Open the selected anime, if multiple files are selected then open as a playlist
        ///     in order of selection
        /// </summary>
        private void Open()
        {
            if (SelectedFiles.Count > 1)
            {
                _playlistService.Set(SelectedFiles);
                _playlistService.Create();
                Process.Start(_playlistService.Path);
            }

            else if (SelectedFile != null)
            {
                Process.Start(SelectedFile.Path);
            }
        }

        /// <summary>
        ///     Go to the anime profile of the selected file.
        /// </summary>
        private void Profile()
        {
            if (SelectedFile == null)
                return;

            var anime = _fileService.ClosestAnime(_animeService.Animes, SelectedFile);

            if (anime != null)
            {
                MessengerInstance.Send(Enums.Views.AnimeDisplay);
                MessengerInstance.Send(anime);
            }
            else
            {
                Methods.Alert($"No anime profile found for {SelectedFile.Name}.");
            }
        }

        /// <summary>
        ///     Delete all selected files.
        /// </summary>
        private void Delete()
        {
            if (SelectedFiles.Count <= 0)
                return;

            var response = MessageBox.Show(
                $"Files to be deleted: \n\n{string.Join("\n", SelectedFiles.Select(ep => ep.Path))}\n\n" +
                "Are you sure?",
                "Confirmation",
                MessageBoxButton.YesNo);

            if (response == MessageBoxResult.Yes)
                foreach (var episode in new List<AnimeFile>(SelectedFiles))
                    FileSystem.DeleteFile(episode.Path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
        }

        /// <summary>
        ///     Open the selected file's MyAnimeList page.
        /// </summary>
        private void MyAnimeListProfile()
        {
            if (SelectedFile == null)
                return;
            var anime = _fileService.ClosestAnime(_animeService.Animes, SelectedFile);
            if (anime != null && anime.MyAnimeList.HasId)
                    Process.Start($"http://myanimelist.net/anime/{anime.MyAnimeList.Id}");
        }

        private void CreateMethodLogic(object sender, FileSystemEventArgs args)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var file = new AnimeFile(args.FullPath);
                Files.AddSorted(file);
                FilteredFiles = new ObservableCollection<AnimeFile>(Files.Where(af => Methods.Strip(af.Name).ToLower().Contains(Methods.Strip(Filter).ToLower())));
                RaisePropertyChanged(nameof(Label));
            });
        }

        private void DeleteMethodLogic(object sender, FileSystemEventArgs args)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var file = Files.First(f => f.Path.Equals(args.FullPath));
                Files.Remove(file);
                if (FilteredFiles.Contains(file))
                    FilteredFiles.Remove(file);
                RaisePropertyChanged(nameof(Label));
            });
        }

        private void RenameMethodLogic(object sender, RenamedEventArgs args)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                MessageBox.Show($"{args.OldFullPath} {args.FullPath}");
                var file = Files.First(f => f.Path.Equals(args.FullPath));
                Files.Remove(file);
                if (FilteredFiles.Contains(file))
                    FilteredFiles.Remove(file);
                RaisePropertyChanged(nameof(Label));
            });
        }
    }
}