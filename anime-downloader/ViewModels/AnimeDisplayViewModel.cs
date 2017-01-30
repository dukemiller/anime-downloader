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
            _settings = settings;
            _animeAggregate = animeAggregate;

            Display = new AnimeListViewModel(_settings, _animeAggregate);

            // Edit single details
            MessengerInstance.Register<Anime>(this,
                anime => { Display = new AnimeDetailsViewModel(_settings, _animeAggregate, anime); });

            // Edit multiple details
            MessengerInstance.Register<ObservableCollection<Anime>>(this,
                animes => { Display = new AnimeDetailsMultipleViewModel(_settings, _animeAggregate, animes); });

            MessengerInstance.Register<NotificationMessage>(this, _ =>
            {
                if (_.Notification.Equals("anime_list"))
                    Display = new AnimeListViewModel(_settings, _animeAggregate);

                // Create new
                else if (_.Notification.Equals("anime_new"))
                    Display = new AnimeDetailsViewModel(_settings, _animeAggregate);

                // Create new multiple
                else if (_.Notification.Equals("anime_newMultiple"))
                    Display = new AnimeDetailsMultipleViewModel(_settings, _animeAggregate);
            });
        }

        private readonly ISettingsService _settings;

        private readonly IAnimeAggregateService _animeAggregate;

        //

        public ViewModelBase Display
        {
            get { return _display; }
            set
            {
                Display?.Cleanup();
                Set(() => Display, ref _display, value);
            }
        }
    }
}