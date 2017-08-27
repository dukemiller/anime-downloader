using System.Collections.Generic;
using System.Windows.Forms;
using anime_downloader.Classes;
using anime_downloader.Models;
using anime_downloader.Models.AniList;
using anime_downloader.ViewModels.Components;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;

namespace anime_downloader.ViewModels
{
    public class AnimeDisplayViewModel : ViewModelBase
    {
        private ViewModelBase _display;

        public AnimeDisplayViewModel()
        {
            // Initial view is the list
            Display = SimpleIoc.Default.GetUniqueInstance<AnimeListViewModel>();

            // Turn an airing anime into an anime model
            MessengerInstance.Register<AiringAnime>(this, 
                anime => Display = SimpleIoc.Default.GetInstance<AnimeDetailsViewModel>().CreateNewFromAiring(anime));

            // Edit single details
            MessengerInstance.Register<Anime>(this, 
                anime => Display = SimpleIoc.Default.GetInstance<AnimeDetailsViewModel>().EditExisting(anime));

            // Edit multiple details
            MessengerInstance.Register<List<Anime>>(this, 
                animes => Display = SimpleIoc.Default.GetInstance<AnimeDetailsMultipleViewModel>().EditExisting(animes));

            MessengerInstance.Register<string>(this, _ =>
            {
                switch (_)
                {
                    case "reset":
                        if (Display.GetType() != typeof(AnimeListViewModel))
                            Display = SimpleIoc.Default.GetUniqueInstance<AnimeListViewModel>();
                        break;

                    case "update":
                        if (Display.GetType() == typeof(AnimeListViewModel))
                            Display = SimpleIoc.Default.GetUniqueInstance<AnimeListViewModel>();
                        break;

                    default:
                        break;
                }
            });

            MessengerInstance.Register<NotificationMessage>(this, _ =>
            {
                switch (_.Notification)
                {
                    case "anime_list":
                        Display = SimpleIoc.Default.GetInstance<AnimeListViewModel>();
                        break;

                    case "anime_new":
                        Display = SimpleIoc.Default.GetInstance<AnimeDetailsViewModel>().CreateNew();
                        break;

                    case "anime_new_multiple":
                        Display = SimpleIoc.Default.GetInstance<AnimeDetailsMultipleViewModel>().CreateNew();
                        break;

                    default:
                        return;
                }
            });
        }

        public ViewModelBase Display
        {
            get => _display;
            set
            {
                Display?.Cleanup();
                Set(() => Display, ref _display, value);
            }
        }
    }
}