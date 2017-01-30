﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            SimpleIoc.Default.Register<IMyAnimeListService, MyAnimeListService>();
            SimpleIoc.Default.Register<IPlaylistService, PlaylistService>();
            SimpleIoc.Default.Register<IAnimeAggregateService, AnimeAggregateService>();

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
            SimpleIoc.Default.Register<DownloadOptionsViewModel>();
            SimpleIoc.Default.Register<DownloaderViewModel>();
            SimpleIoc.Default.Register<DownloadLogViewModel>();
        }

        public static MainWindowViewModel Main => ServiceLocator.Current.GetInstance<MainWindowViewModel>();
    }
}