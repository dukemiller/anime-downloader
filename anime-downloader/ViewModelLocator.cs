using System;
using anime_downloader.Enums;
using anime_downloader.Repositories;
using anime_downloader.Repositories.Interface;
using anime_downloader.Services;
using anime_downloader.Services.Interfaces;
using anime_downloader.ViewModels;
using anime_downloader.ViewModels.Components;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;

namespace anime_downloader
{
    public class ViewModelLocator
    {
        static ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            // Repositories
            SimpleIoc.Default.Register<ISettingsRepository>(SettingsRepository.Load);
            SimpleIoc.Default.Register<ICredentialsRepository>(CredentialsRepository.Load);
            SimpleIoc.Default.Register<IAnimeRepository>(AnimeRepository.Load);

            // Services (order is important)
            SimpleIoc.Default.Register<IAnimeService, AnimeService>();
            RegisterIDownloadService();
            SimpleIoc.Default.Register<IFileService, FileService>();
            SimpleIoc.Default.Register<IMyAnimeListApi, MyAnimeListInternalApi>();
            SimpleIoc.Default.Register<IMyAnimeListService, MyAnimeListService>();
            SimpleIoc.Default.Register<IAniListApi, AniListApi>();
            SimpleIoc.Default.Register<IFindSeasonAnimeService, AniListService>();
            SimpleIoc.Default.Register<IVersionService, VersionService>();
            SimpleIoc.Default.Register<IDetailProviderService, AniListDetailProviderService>();

            // Viewmodels
            SimpleIoc.Default.Register<MainWindowViewModel>();
            SimpleIoc.Default.Register<DiscoverViewModel>();
            SimpleIoc.Default.Register<AnimeDisplayViewModel>();
            SimpleIoc.Default.Register<DownloadViewModel>();
            SimpleIoc.Default.Register<HomeViewModel>();
            SimpleIoc.Default.Register<ManageViewModel>();
            SimpleIoc.Default.Register<MiscViewModel>();
            SimpleIoc.Default.Register<PlaylistCreatorViewModel>();
            SimpleIoc.Default.Register<SettingsViewModel>();
            SimpleIoc.Default.Register<WebViewModel>();

            // Components
            SimpleIoc.Default.Register<AnimeDetailsMultipleViewModel>();
            SimpleIoc.Default.Register<AnimeDetailsViewModel>();
            SimpleIoc.Default.Register<AnimeListViewModel>();
            SimpleIoc.Default.Register<DownloaderViewModel>();
            SimpleIoc.Default.Register<DownloadLogViewModel>();
            SimpleIoc.Default.Register<DownloadOptionsViewModel>();
            SimpleIoc.Default.Register<DetailsBarViewModel>();
        }

        public static void RegisterIDownloadService()
        {
            SimpleIoc.Default.Unregister<IDownloadService>();

            switch (ServiceLocator.Current.GetInstance<ISettingsRepository>().Provider)
            {
                case DownloadProvider.NyaaPantsu:
                    SimpleIoc.Default.Register<IDownloadService, NyaaPantsuService>();
                    break;
                case DownloadProvider.NyaaSi:
                    SimpleIoc.Default.Register<IDownloadService, NyaaSiService>();
                    break;
                case DownloadProvider.HorribleSubs:
                    SimpleIoc.Default.Register<IDownloadService, HorribleSubsService>();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static MainWindowViewModel Main => ServiceLocator.Current.GetInstance<MainWindowViewModel>();

        public static DiscoverViewModel Discover => ServiceLocator.Current.GetInstance<DiscoverViewModel>();

        public static AnimeDisplayViewModel Anime => ServiceLocator.Current.GetInstance<AnimeDisplayViewModel>();

        public static SettingsViewModel Settings => ServiceLocator.Current.GetInstance<SettingsViewModel>();

        public static DownloadViewModel Download => ServiceLocator.Current.GetInstance<DownloadViewModel>();

        public static HomeViewModel Home => ServiceLocator.Current.GetInstance<HomeViewModel>();

        public static MiscViewModel Misc => ServiceLocator.Current.GetInstance<MiscViewModel>();

        public static ManageViewModel Manage => ServiceLocator.Current.GetInstance<ManageViewModel>();

        public static PlaylistCreatorViewModel Playlist => ServiceLocator.Current.GetInstance<PlaylistCreatorViewModel>();

        public static WebViewModel Web => ServiceLocator.Current.GetInstance<WebViewModel>();

        public static bool Loaded => true;
    }
}
