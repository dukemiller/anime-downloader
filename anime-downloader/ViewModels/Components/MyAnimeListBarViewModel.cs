using System;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Windows;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Services.Interfaces;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace anime_downloader.ViewModels.Components
{
    public class MyAnimeListBarViewModel : ViewModelBase
    {
        private Anime _anime;

        private readonly ISettingsService _settings;

        private readonly IMyAnimeListService _malService;

        // 

        public MyAnimeListBarViewModel(ISettingsService settings, IMyAnimeListService malService)
        {
            _settings = settings;
            _malService = malService;

            FindCommand = new RelayCommand(Find, () => _settings.MyAnimeListConfig.Works);
            ClearCommand = new RelayCommand(Clear);
            ProfileCommand = new RelayCommand(Profile);
            RefreshCommand = new RelayCommand(Refresh);

            _settings.MyAnimeListConfig.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName.Equals("Works"))
                    RaisePropertyChanged(nameof(LoggedIntoMal));
            };
        }

        public MyAnimeListBarViewModel Load(Anime anime)
        {
            _anime = anime;

            _anime.MyAnimeList.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName.Equals("Id"))
                    RaisePropertyChanged(nameof(HasId));
            };

            return this;
        }

        // 

        public bool LoggedIntoMal => _settings.MyAnimeListConfig.Works;

        public Visibility HasId => _anime.MyAnimeList.HasId ? Visibility.Visible : Visibility.Collapsed;

        public RelayCommand FindCommand { get; set; }

        public RelayCommand ClearCommand { get; set; }

        public RelayCommand ProfileCommand { get; set; }

        public RelayCommand RefreshCommand { get; set; }

        // 

        private async void Find()
        {
            MessengerInstance.Send(new WorkMessage {Working = true});
            var id = await _malService.GetId(_anime);
            RaisePropertyChanged(nameof(HasId));
            _settings.Save();
            if (!id)
                Methods.Alert($"No ID found for {_anime.Name}.");
            MessengerInstance.Send(new WorkMessage {Working = false});
        }

        private void Clear()
        {
            var response = MessageBox.Show("This will delete all saved MyAnimeList data about this show, are you sure?",
                "Confirmation",
                MessageBoxButton.YesNo);
            if (response == MessageBoxResult.Yes)
            {
                _anime.MyAnimeList = new MyAnimeListDetails {Id = null, NeedsUpdating = true};
                RaisePropertyChanged(nameof(HasId));
                _settings.Save();
            }
        }

        private void Profile() => Process.Start($"http://myanimelist.net/anime/{_anime.MyAnimeList.Id}");

        private async void Refresh()
        {
            MessengerInstance.Send(new WorkMessage {Working = true});
            if (!await _malService.Refresh(_anime))
                Methods.Alert("Had trouble finding this show on MAL.");
            RaisePropertyChanged(nameof(HasId));
            MessengerInstance.Send(new WorkMessage {Working = false});
        }
    }
}