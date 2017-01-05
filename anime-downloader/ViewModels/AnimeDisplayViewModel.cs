using System.Collections.ObjectModel;
using anime_downloader.Models;
using anime_downloader.Services.Interfaces;
using anime_downloader.ViewModels.Components;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;

namespace anime_downloader.ViewModels
{
    public class AnimeDisplayViewModel : ViewModelBase
    {
        private ViewModelBase _display;

        //

        public AnimeDisplayViewModel(ISettingsService settings, IAnimeAggregateService animeAggregate)
        {
            Settings = settings;
            AnimeAggregate = animeAggregate;

            Display = new AnimeListViewModel(Settings, AnimeAggregate);

            // Edit single details
            MessengerInstance.Register<Anime>(this,
                anime => { Display = new AnimeDetailsViewModel(Settings, AnimeAggregate, anime); });

            // Edit multiple details
            MessengerInstance.Register<ObservableCollection<Anime>>(this,
                animes => { Display = new AnimeDetailsMultipleViewModel(Settings, AnimeAggregate, animes); });

            MessengerInstance.Register<NotificationMessage>(this, _ =>
            {
                if (_.Notification.Equals("anime_list"))
                    Display = new AnimeListViewModel(Settings, AnimeAggregate);

                // Create new
                else if (_.Notification.Equals("anime_new"))
                    Display = new AnimeDetailsViewModel(Settings, AnimeAggregate);

                // Create new multiple
                else if (_.Notification.Equals("anime_newMultiple"))
                    Display = new AnimeDetailsMultipleViewModel(Settings, AnimeAggregate);
            });
        }

        private ISettingsService Settings { get; }
        private IAnimeAggregateService AnimeAggregate { get; }

        //

        public ViewModelBase Display
        {
            get { return _display; }
            set { Set(() => Display, ref _display, value); }
        }
    }
}