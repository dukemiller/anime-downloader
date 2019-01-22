using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Repositories.Interface;
using anime_downloader.Services.Interfaces;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using HtmlAgilityPack;
using Component = anime_downloader.Enums.Component;

namespace anime_downloader.ViewModels.Components.AnimeDisplay
{
    public class AnimeListViewModel : ViewModelBase
    {
        private readonly IAnimeService _animeService;
        private ObservableCollection<Anime> _animes;
        private string _filterText;
        private FindViewModel _find;
        private Anime _selectedAnime;
        private ISettingsRepository _settings;

        // 

        public AnimeListViewModel(ISettingsRepository settings, IAnimeService animeService, ICredentialsRepository credentialsRepository)
        {
            _animeService = animeService;
            Settings = settings;
            CredentialsRepository = credentialsRepository;
            CredentialsRepository.PropertyChanged -= ReloadList;
            CredentialsRepository.PropertyChanged += ReloadList;

            // 

            Find = new FindViewModel();
            Find.PropertyChanged -= UpdateListBasedOnFind;
            Find.PropertyChanged += UpdateListBasedOnFind;

            FilterText = Settings.FilterBy;
            Animes = new ObservableCollection<Anime>(_animeService.FilteredAndSorted());

            // 

            SelectionChangedCommand = new RelayCommand<IList>(items =>
            {
                if (items == null)
                    return;
                SelectedAnimes = items.Cast<Anime>().ToList();
            });

            FindToggleCommand = new RelayCommand(() => Find.Toggle());

            AddCommand = new RelayCommand(Add);
            AddMultipleCommand = new RelayCommand(AddMultiple);
            EditCommand = new RelayCommand(Edit);
            DeleteCommand = new RelayCommand(Delete);
            SearchCommand = new RelayCommand(Search);
            CopyCommand = new RelayCommand(() =>
            {
                Clipboard.Clear();
                Clipboard.SetText(string.Join(", ", SelectedAnimes.Select(c => c.Title)));
            });
        }

        // 

        public string FilterText
        {
            get => _filterText;
            set
            {
                _filterText = value;
                Settings.FilterBy = value;
                Settings.Save();
                Animes = new ObservableCollection<Anime>(_animeService.FilteredAndSorted());
            }
        }

        public string Stats
        {
            get
            {
                var anime = _animeService.Animes.ToList();
                return $"{anime.Count} total. " +
                       $"{anime.Count(a => a.Airing && a.Status == Status.Watching)} airing/watching, " +
                       $"{anime.Count(a => a.Status == Status.Finished)} finished, " +
                       $"{anime.Count(a => a.Status == Status.OnHold || a.Status == Status.Considering)} on hold/considering, " +
                       $"{anime.Count(a => a.Status == Status.Dropped)} dropped.";
            }
        }

        public FindViewModel Find
        {
            get => _find;
            set => Set(() => Find, ref _find, value);
        }

        public Anime SelectedAnime
        {
            get => _selectedAnime;
            set => Set(() => SelectedAnime, ref _selectedAnime, value);
        }

        public ObservableCollection<Anime> Animes
        {
            get => _animes;
            set => Set(() => Animes, ref _animes, value);
        }

        private List<Anime> SelectedAnimes { get; set; } = new List<Anime>();

        public ISettingsRepository Settings
        {
            get => _settings;
            set => Set(() => Settings, ref _settings, value);
        }

        public ICredentialsRepository CredentialsRepository { get; }

        public RelayCommand AddCommand { get; set; }

        public RelayCommand AddMultipleCommand { get; set; }

        public RelayCommand EditCommand { get; set; }

        public RelayCommand DeleteCommand { get; set; }

        public RelayCommand SearchCommand { get; set; }

        public RelayCommand FindToggleCommand { get; set; }

        public RelayCommand CopyCommand { get; set; }

        public RelayCommand<IList> SelectionChangedCommand { get; set; }

        // 

        private void Edit()
        {
            if (SelectedAnimes?.Count > 1)
                MessengerInstance.Send(SelectedAnimes);

            else if (SelectedAnimes?.Count == 1)
                MessengerInstance.Send(SelectedAnime);
        }

        private void Delete()
        {
            foreach (var anime in new List<Anime>(SelectedAnimes))
            {
                _animeService.Remove(anime);
                Animes.Remove(anime);
            }

            RaisePropertyChanged(nameof(Stats));
            MessengerInstance.Send(ViewRequest.Refresh);
            _find.Close();
        }

        private async void Search()
        {
            if (SelectedAnime == null)
                return;
            if (SelectedAnime.Details.HasId)
                Process.Start($"http://myanimelist.net/anime/{SelectedAnime.Details.Id}");
            else
            {

                MessengerInstance.Send(ViewState.IsWorking);
                await SearchAndOpenAsync(SelectedAnime.Name);
                MessengerInstance.Send(ViewState.DoneWorking);
            }
        }

        private void Add() => MessengerInstance.Send(Component.Details);

        private void AddMultiple() => MessengerInstance.Send(Component.DetailsMultiple);

        private static async Task SearchAndOpenAsync(string text)
        {
            var q = HttpUtility.UrlEncode(text);
            var document = new HtmlDocument();

            using (var client = new WebClient())
            {
                var html = await client.DownloadStringTaskAsync(new Uri($"https://myanimelist.net/anime.php?q={q}"));
                document.LoadHtml(html);
            }

            var link = document.DocumentNode?
                .SelectSingleNode("//div[@class=\"js-categories-seasonal js-block-list list\"]/table/tr[2]/td[1]")?
                .Descendants("a")?
                .FirstOrDefault();

            if (link != null)
                Process.Start(link.Attributes["href"].Value);
            else
                Methods.Alert("No results found.");
        }

        private void ReloadList(object sender, PropertyChangedEventArgs args)
        {
            Animes = new ObservableCollection<Anime>(_animeService.FilteredAndSorted());
        }

        private void UpdateListBasedOnFind(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName.Equals("Text"))
                Animes = Find.Text.Equals("")
                    ? new ObservableCollection<Anime>(_animeService.FilteredAndSorted())
                    : new ObservableCollection<Anime>(_animeService.FilteredAndSorted()
                        .Where(a => a.Name.ToLower().Contains(Find.Text.ToLower())));
        }
    }
}