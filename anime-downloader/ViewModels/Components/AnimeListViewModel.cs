using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using anime_downloader.Classes;
using anime_downloader.Models;
using anime_downloader.Services.Interfaces;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using HtmlAgilityPack;

namespace anime_downloader.ViewModels.Components
{
    public class AnimeListViewModel : ViewModelBase
    {
        private FindViewModel _find;
        private string _filterText;
        private Anime _selectedAnime;
        private ObservableCollection<Anime> _animes;
        private ISettingsService _settings;
        private IAnimeAggregateService _animeAggregate;

        // 

        public AnimeListViewModel(ISettingsService settings, IAnimeAggregateService animeAggregate)
        {
            Settings = settings;
            AnimeAggregate = animeAggregate;

            // 
            
            Find = new FindViewModel();
            Find.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName.Equals("Text"))
                {
                    if (Find.Text.Equals(""))
                        Animes = new ObservableCollection<Anime>(AnimeAggregate.Animes.FilteredAndSorted());
                    else
                        Animes =
                            new ObservableCollection<Anime>(
                                AnimeAggregate.Animes.Animes.Where(a => a.Name.ToLower().Contains(Find.Text.ToLower())));
                }
            };

            FilterText = Settings.FilterBy;
            Animes = new ObservableCollection<Anime>(AnimeAggregate.Animes.FilteredAndSorted());
            
            // 

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
            get { return _filterText; }
            set
            {
                _filterText = value;
                Settings.FilterBy = value;
                Settings.Save();
                Animes = new ObservableCollection<Anime>(AnimeAggregate.Animes.FilteredAndSorted());
            }
        }

        public string Stats
        {
            get {
                var anime = AnimeAggregate.Animes.Animes.ToList();
                return $"{anime.Count} total. " +
                       $"{anime.Count(a => a.Airing && a.Status.Equals("Watching"))} airing/watching, " +
                       $"{anime.Count(a => a.Status.Equals("Finished"))} finished, " +
                       $"{anime.Count(a => a.Status.Equals("On Hold") || a.Status.Equals("Considering"))} on hold/considering, " +
                       $"{anime.Count(a => a.Status.Equals("Dropped"))} dropped.";
            }
        }

        public FindViewModel Find
        {
            get { return _find; }
            set { Set(() => Find, ref _find, value); }
        }
        
        public Anime SelectedAnime
        {
            get { return _selectedAnime; }
            set { Set(() => SelectedAnime, ref _selectedAnime, value); }
        }
        
        public ObservableCollection<Anime> Animes
        {
            get { return _animes; }
            set { Set(() => Animes, ref _animes, value); }
        }
        
        public ObservableCollection<Anime> SelectedAnimes { get; set; } = new ObservableCollection<Anime>();

        public ISettingsService Settings
        {
            get { return _settings; }
            set { Set(() => Settings, ref _settings, value); }
        }

        public IAnimeAggregateService AnimeAggregate
        {
            get { return _animeAggregate; }
            set { Set(() => AnimeAggregate, ref _animeAggregate, value); }
        }

        public RelayCommand AddCommand { get; set; }

        public RelayCommand AddMultipleCommand { get; set; }

        public RelayCommand EditCommand { get; set; }

        public RelayCommand DeleteCommand { get; set; }

        public RelayCommand SearchCommand { get; set; }

        public RelayCommand FindToggleCommand { get; set; }

        public RelayCommand CopyCommand { get; set; }

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
                if (!anime.Status.Equals("Dropped") && (anime.MyAnimeList.HasId || anime.Episode > 0 || anime.HasRating))
                {
                    anime.Status = "Dropped";
                    anime.Airing = false;
                }

                else
                {
                    AnimeAggregate.Animes.Remove(anime);
                    Animes.Remove(anime);
                }
            }
            RaisePropertyChanged(nameof(Stats));
            _find.Close();
        }

        private async void Search()
        {
            if (SelectedAnime == null)
                return;
            if (SelectedAnime.MyAnimeList.HasId)
                Process.Start($"http://myanimelist.net/anime/{SelectedAnime.MyAnimeList.Id}");
            else
            {
                MessengerInstance.Send(new WorkMessage {Working = true});
                await SearchAndOpenAsync(SelectedAnime.Name);
                MessengerInstance.Send(new WorkMessage {Working = false});
            }
        }

        private void Add() => MessengerInstance.Send(new NotificationMessage("anime_new"));

        private void AddMultiple() => MessengerInstance.Send(new NotificationMessage("anime_newMultiple"));

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

    }
}
