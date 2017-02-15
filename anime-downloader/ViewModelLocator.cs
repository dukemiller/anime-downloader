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

            // Services (order is important)
            SimpleIoc.Default.Register<ISettingsService>(new XmlSettingsService().Load);
            SimpleIoc.Default.Register<IAnimeService, AnimeService>();
            SimpleIoc.Default.Register<IDownloadService, NyaaService>();
            SimpleIoc.Default.Register<IFileService, FileService>();
            SimpleIoc.Default.Register<IMyAnimeListApi, MyAnimeListApi>();
            SimpleIoc.Default.Register<IMyAnimeListService, MyAnimeListService>();
            SimpleIoc.Default.Register<IPlaylistService, PlaylistService>();

            // Viewmodels
            SimpleIoc.Default.Register<MainWindowViewModel>();
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
            SimpleIoc.Default.Register<MyAnimeListBarViewModel>();
        }

        public static MainWindowViewModel Main => ServiceLocator.Current.GetInstance<MainWindowViewModel>();

    }
}
