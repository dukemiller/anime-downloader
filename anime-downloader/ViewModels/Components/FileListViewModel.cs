using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Services.Interfaces;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.VisualBasic.FileIO;
using Optional;
using Optional.Collections;
using PropertyChanged;

namespace anime_downloader.ViewModels.Components
{
    public class FileListViewModel : ViewModelBase
    {
        private readonly IFileService _fileService;

        private readonly IAnimeService _animeService;

        // 

        public FileListViewModel(IFileService fileService, IAnimeService animeService)
        {
            _fileService = fileService;
            _animeService = animeService;

            SelectionChangedCommand = new RelayCommand<IList>(items =>
            {
                if (items is null)
                    return;
                SelectedFiles = items.Cast<AnimeFile>().ToList();
            });
        }

        // 

        public RelayCommand<IList> SelectionChangedCommand { get; set; }

        private FileSystemWatcher Watcher { get; set; }

        /// <summary>
        ///     The parent files that the source retrives views from
        /// </summary>
        public ObservableCollection<AnimeFile> Files { get; set; } = new ObservableCollection<AnimeFile>();

        /// <summary>
        ///     A label simply showing the count of total files
        /// </summary>
        [DependsOn(nameof(FilteredFiles))]
        public string Label => $"({FilteredFiles?.Count ?? 0} files)";

        /// <summary>
        ///     A user flag to note if the file count should be hidden or not
        /// </summary>
        public bool HideLabel { get; set; }

        /// <summary>
        ///     The actual views display of files
        /// </summary>
        [DependsOn(nameof(Filter), nameof(Files))]
        public ObservableCollection<AnimeFile> FilteredFiles => Filter?.Length == 0
            ? Files
            : new ObservableCollection<AnimeFile>(Files.Where(file => Methods.Strip(file.Name).ToLower().Contains(Methods.Strip(Filter).ToLower())));

        /// <summary>
        ///     Either the only selected file or the first selected of a group of files
        /// </summary>
        public AnimeFile SelectedFile { get; set; }

        /// <summary>
        ///     All selected files
        /// </summary>
        public List<AnimeFile> SelectedFiles { get; set; } = new List<AnimeFile>();

        /// <summary>
        ///     The episode type that will determine what episodes are filled in the Files list
        /// </summary>
        public EpisodeStatus? EpisodeType { get; set; }

        /// <summary>
        ///     The target destination
        /// </summary>
        public string MovePath { get; set; } = "";

        /// <summary>
        ///     The source destination
        /// </summary>
        public string StartPath { get; set; } = "";

        /// <summary>
        ///     Display title at the top of the control
        /// </summary>
        public string Title { get; set; } = "";

        /// <summary>
        ///     Text that determines the FilteredFiles list subset of the Files list
        /// </summary>
        public string Filter { get; set; } = "";

        /// <summary>
        ///     The path for the image representing which direction the files will move
        /// </summary>
        public string ImageResourcePath { get; set; } = "";

        public RelayCommand FolderCommand => new RelayCommand(() => Process.Start(StartPath));

        public RelayCommand MoveCommand => new RelayCommand(Move);

        public RelayCommand ProfileCommand => new RelayCommand(Profile);

        public RelayCommand MalCommand => new RelayCommand(MyAnimeListProfile);

        public RelayCommand OpenCommand => new RelayCommand(Open);

        public RelayCommand DeleteCommand => new RelayCommand(Delete);

        public RelayCommand CopyCommand => new RelayCommand(Copy);

        public RelayCommand ClearFilterCommand => new RelayCommand(() => Filter = "");

        // 

        private void OnEpisodeTypeChanged()
        {
            if (EpisodeType is null)
                return;

            Files = new ObservableCollection<AnimeFile>(_fileService.GetEpisodes(EpisodeType.Value));
            Files.CollectionChanged += (sender, args) =>
            {
                RaisePropertyChanged(nameof(FilteredFiles));
                RaisePropertyChanged(nameof(Label));
            };
        }

        private void OnStartPathChanged()
        {
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

        /// <summary>
        ///     Move all selected files from from {StartPath} to {MovePath}
        /// </summary>
        private void Move()
        {
            if (SelectedFiles.Count > 0 && Directory.Exists(MovePath))
                foreach (var file in SelectedFiles)
                    Methods.MoveFile(file, StartPath, MovePath);
        }

        /// <summary>
        ///     Open the selected anime, if multiple files are selected then open as a playlist
        ///     in order of selection
        /// </summary>
        private async void Open()
        {
            if (SelectedFiles.Count > 1)
            {
                var playlist = new Playlist
                {
                    Source = SelectedFiles,
                    Sort = false,
                    IsEpisodeSelection = true
                };
                if (await playlist.Create())
                    Process.Start(playlist.Path);
            }

            else if (SelectedFile != null)
                Process.Start(SelectedFile.Path);
        }

        /// <summary>
        ///     Go to the anime profile of the selected file.
        /// </summary>
        private void Profile()
        {
            if (SelectedFile is null)
                return;

            _fileService
                .ClosestAnime(_animeService.WatchingOrCompleted, SelectedFile, Tolerance.High)
                .Match(
                    some: anime =>
                    {
                        MessengerInstance.Send(Display.Anime);
                        MessengerInstance.Send(anime);
                    },
                    none: () => Methods.Alert($"No anime profile found for {SelectedFile.Name}.")
                );
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
        ///     Copy selected stripped filenames to the clipboard.
        /// </summary>
        private void Copy()
        {
            Clipboard.Clear();
            Clipboard.SetText(string.Join(", ", SelectedFiles.Select(c => c.StrippedFilename)));
        }

        /// <summary>
        ///     Open the selected file's MyAnimeList page.
        /// </summary>
        private void MyAnimeListProfile()
            => SelectedFile.SomeNotNull().MatchSome(file =>
                _fileService
                    .ClosestAnime(_animeService.Animes, file, Tolerance.Low)
                    .Filter(anime => anime.Details.HasId)
                    .MatchSome(anime => Process.Start($"http://myanimelist.net/anime/{anime.Details.Id}")));

        private void CreateMethodLogic(object sender, FileSystemEventArgs args)
            => Application.Current.Dispatcher.InvokeAsync(()
                => args
                    .SomeNotNull()
                    .Map(arg => arg.FullPath)
                    .MatchSome(path => Files.AddSorted(new AnimeFile(path))));

        private void DeleteMethodLogic(object sender, FileSystemEventArgs args) 
            => Application.Current.Dispatcher.InvokeAsync(() 
                => args
                    .SomeNotNull()
                    .Map(arg => arg.FullPath)
                    .MatchSome(path => Files.FirstOrNone(file => file.Path == path).MatchSome(file => Files.Remove(file))));

        private void RenameMethodLogic(object sender, RenamedEventArgs args) 
            => Application.Current.Dispatcher.InvokeAsync(() 
                => args
                    .SomeNotNull()
                    .Map(arg => arg.FullPath)
                    .MatchSome(path => Files.FirstOrNone(file => file.Path == path).MatchSome(file => Files.Remove(file))));
    }
}