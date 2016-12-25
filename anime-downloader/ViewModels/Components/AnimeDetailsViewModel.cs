using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Windows;
using anime_downloader.Classes;
using anime_downloader.Models;
using anime_downloader.Services.Interfaces;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;

namespace anime_downloader.ViewModels.Components
{
    public class AnimeDetailsViewModel : ViewModelBase
    {
        private Anime _anime;
        private RelayCommand _buttonCommand;
        private string _buttonText;
        private string _selectedSubgroup;
        private ISettingsService _settings;
        private MyAnimeListBarViewModel _myAnimeListBar;

        // 

        /// <summary>
        ///     Edit
        /// </summary>
        public AnimeDetailsViewModel(ISettingsService settings, IAnimeAggregateService animeAggregate, Anime anime)
        {
            Settings = settings;
            AnimeAggregate = animeAggregate;
            Anime = anime;
            ButtonText = "Edit";
            ButtonCommand = new RelayCommand(
                Edit,
                () => !AnimeAggregate.Animes.Animes.Except(new []{Anime}).Any(a => a.Name.ToLower().Trim().Equals(Anime?.Name?.ToLower().Trim()))
                      && Anime?.Name?.Length > 0
            );
            ExitCommand = new RelayCommand(() => MessengerInstance.Send(Enums.Views.AnimeDisplay));
            MyAnimeListBar = new MyAnimeListBarViewModel(Anime, Settings, AnimeAggregate);
        }

        /// <summary>
        ///     Create
        /// </summary>
        public AnimeDetailsViewModel(ISettingsService settings, IAnimeAggregateService animeAggregate)
        {
            Settings = settings;
            AnimeAggregate = animeAggregate;
            Anime = new Anime
            {
                Episode = 0,
                Status = "Watching",
                Resolution = "720",
                Airing = true
            };
            ButtonText = "Add";
            ButtonCommand = new RelayCommand(
                Create,
                () => !AnimeAggregate.Animes.Animes.Any(a => a.Name.ToLower().Trim().Equals(Anime?.Name?.ToLower().Trim()))
                      && Anime?.Name?.Length > 0
            );
            ExitCommand = new RelayCommand(() => MessengerInstance.Send(Enums.Views.AnimeDisplay));
        }

        // 

        public ISettingsService Settings
        {
            get { return _settings; }
            set { Set(() => Settings, ref _settings, value); }
        }

        private IAnimeAggregateService AnimeAggregate { get; }

        public string ButtonText
        {
            get { return _buttonText; }
            set { Set(() => ButtonText, ref _buttonText, value); }
        }

        public RelayCommand ButtonCommand
        {
            get { return _buttonCommand; }
            set { Set(() => ButtonCommand, ref _buttonCommand, value); }
        }

        public RelayCommand ExitCommand { get; set; }

        public MyAnimeListBarViewModel MyAnimeListBar
        {
            get { return _myAnimeListBar; }
            set { Set(() => MyAnimeListBar, ref _myAnimeListBar, value); }
        }

        public string SelectedSubgroup
        {
            get { return _selectedSubgroup; }
            set { Set(() => SelectedSubgroup, ref _selectedSubgroup, value); }
        }

        public Anime Anime
        {
            get { return _anime; }
            set
            {
                Set(() => Anime, ref _anime, value);
                Anime.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName.Equals("Name"))
                        ButtonCommand.RaiseCanExecuteChanged();
                };
            }
        }
        
        // 

        private void Edit()
        {
            Settings.Save();
            MessengerInstance.Send(new NotificationMessage("anime_list"));
        }

        private void Create()
        {
            if (!string.IsNullOrEmpty(SelectedSubgroup))
                Anime.PreferredSubgroup = SelectedSubgroup;
            Settings.Animes.Add(Anime);
            MessengerInstance.Send(new NotificationMessage("anime_list"));
        }
    }
}