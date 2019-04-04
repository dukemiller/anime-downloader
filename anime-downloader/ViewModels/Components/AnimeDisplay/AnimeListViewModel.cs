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
using Optional;
using Optional.Collections;
using Component = anime_downloader.Enums.Component;

namespace anime_downloader.ViewModels.Components.AnimeDisplay
{
    public class AnimeListViewModel : ViewModelBase
    {
        private readonly IAnimeService _animeService;

        // 

        public AnimeListViewModel(ISettingsRepository settings, 
            IAnimeService animeService,
            ICredentialsRepository credentialsRepository)
        {
            _animeService = animeService;
            Settings = settings;

            // Add listeners
            CredentialsRepository = credentialsRepository;
            CredentialsRepository.PropertyChanged -= ReloadList;
            CredentialsRepository.PropertyChanged += ReloadList;
            Find.PropertyChanged -= UpdateListBasedOnFind;
            Find.PropertyChanged += UpdateListBasedOnFind;

            // 

            FilterText = Settings.FilterBy;
            Animes = new ObservableCollection<Anime>(_animeService.FilteredAndSorted());

            // 

            SelectionChangedCommand = new RelayCommand<IList>(items =>
            {
                if (items == null)
                    return;
                SelectedAnimes = items.Cast<Anime>().ToList();
            });

            MessengerInstance.Register<ViewRequest>(this, request =>
            {
                if (request == ViewRequest.Refresh)
                    Animes = new ObservableCollection<Anime>(_animeService.FilteredAndSorted());
            });
        }

        // 

        public string FilterText { get; set; }

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

        public ISettingsRepository Settings { get; set; }

        public ICredentialsRepository CredentialsRepository { get; }

        public FindViewModel Find { get; set; } = new FindViewModel();

        public Anime SelectedAnime { get; set; }

        public ObservableCollection<Anime> Animes { get; set; }

        private List<Anime> SelectedAnimes { get; set; } = new List<Anime>();

        public RelayCommand AddCommand => new RelayCommand(Add);

        public RelayCommand AddMultipleCommand => new RelayCommand(AddMultiple);

        public RelayCommand EditCommand => new RelayCommand(Edit);

        public RelayCommand DeleteCommand => new RelayCommand(Delete);

        public RelayCommand SearchCommand => new RelayCommand(Search);

        public RelayCommand FindToggleCommand => new RelayCommand(() => Find.Toggle());

        public RelayCommand CopyCommand => new RelayCommand(Copy);

        public RelayCommand<IList> SelectionChangedCommand { get; set; }

        // 

        private void OnFilterTextChanged()
        {
            if (Settings.FilterBy == FilterText)
                return;
            Settings.FilterBy = FilterText;
            Settings.Save();
            Animes = new ObservableCollection<Anime>(_animeService.FilteredAndSorted());
        }

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
            Find.Close();
        }

        private async void Search()
        {
            if (SelectedAnime is null)
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

        private void Copy()
        {
            Clipboard.Clear();
            Clipboard.SetText(string.Join(", ", SelectedAnimes.Select(c => c.Title)));
        }

        private static async Task SearchAndOpenAsync(string text)
        {
            var q = HttpUtility.UrlEncode(text);
            var document = new HtmlDocument();

            using (var client = new WebClient())
            {
                var html = await client.DownloadStringTaskAsync(new Uri($"https://myanimelist.net/anime.php?q={q}"));
                document.LoadPage(html).DocumentNode
                    .SelectSingleNode("//div[@class=\"js-categories-seasonal js-block-list list\"]/table/tr[2]/td[1]")
                    .SomeNotNull()
                    .FlatMap(node => node.Descendants("a").FirstOrNone()).Match(
                        some: link => Process.Start(link.Attributes["href"].Value),
                        none: () => Methods.Alert("No results found.")
                    );
            }
        }

        private void ReloadList(object sender, PropertyChangedEventArgs args)
        {
            Animes = new ObservableCollection<Anime>(_animeService.FilteredAndSorted());
        }

        private void UpdateListBasedOnFind(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "Text")
                Animes = Find.Text == ""
                    ? new ObservableCollection<Anime>(_animeService.FilteredAndSorted())
                    : new ObservableCollection<Anime>(_animeService.FilteredAndSorted()
                        .Where(a => a.Name.ToLower().Contains(Find.Text.ToLower())));
        }
    }
}