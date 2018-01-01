using System.Collections.Generic;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Models.AniList;
using anime_downloader.ViewModels.Components.AnimeDisplay;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;

namespace anime_downloader.ViewModels.Displays
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
                anime => Display = SimpleIoc.Default.GetInstance<DetailsViewModel>().CreateNewFromAiring(anime));

            // Edit single details
            MessengerInstance.Register<Anime>(this, 
                anime => Display = SimpleIoc.Default.GetInstance<DetailsViewModel>().EditExisting(anime));

            // Edit multiple details
            MessengerInstance.Register<List<Anime>>(this, 
                animes => Display = SimpleIoc.Default.GetInstance<DetailsMultipleViewModel>().EditExisting(animes));

            MessengerInstance.Register<ViewRequest>(this, HandleViewRequest);
            MessengerInstance.Register<Component>(this, HandleComponent);
        }

        private void HandleComponent(Component _)
        {
            switch (_)
            {
                case Component.AnimeList:
                    Display = SimpleIoc.Default.GetInstance<AnimeListViewModel>();
                    break;

                case Component.Details:
                    Display = SimpleIoc.Default.GetInstance<DetailsViewModel>().CreateNew();
                    break;

                case Component.DetailsMultiple:
                    Display = SimpleIoc.Default.GetInstance<DetailsMultipleViewModel>().CreateNew();
                    break;

                default:
                    return;
            }
        }

        private void HandleViewRequest(ViewRequest vr)
        {
            switch (vr)
            {
                case ViewRequest.Reset:
                    if (Display.GetType() != typeof(AnimeListViewModel))
                        Display = SimpleIoc.Default.GetUniqueInstance<AnimeListViewModel>();
                    break;

                case ViewRequest.Update:
                    if (Display.GetType() == typeof(AnimeListViewModel))
                        Display = SimpleIoc.Default.GetUniqueInstance<AnimeListViewModel>();
                    break;

                default:
                    break;
            }
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