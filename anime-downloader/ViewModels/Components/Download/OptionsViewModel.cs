using System.Collections.Generic;
using System.Linq;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Repositories.Interface;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using static anime_downloader.Classes.Methods;

namespace anime_downloader.ViewModels.Components.Download
{
    public class OptionsViewModel : ViewModelBase
    {
        private readonly ISettingsRepository _settings;

        private static readonly Radio<DownloadOption> NextEpisode = new Radio<DownloadOption>
        {
            Header = "Search for next found episode",
            Data = DownloadOption.Next
        };

        private static readonly Radio<DownloadOption> Continually = new Radio<DownloadOption>
        {
            Header = "Continually search until no more are found (good for getting up to date)",
            Data = DownloadOption.Continually
        };

        private static readonly Radio<DownloadOption> Missing = new Radio<DownloadOption>
        {
            Header = "Download any missing episodes between first and last downloaded episode",
            Data = DownloadOption.Missing
        };
        
        // 

        public OptionsViewModel(ISettingsRepository settings)
        {
            _settings = settings;
            CurrentProvider = _settings.Provider;
            SelectedRadio = Options.First();
        }

        // 

        public List<Radio<DownloadOption>> Options { get; set; } = List.Of(NextEpisode, Continually, Missing);

        public List<DownloadProvider> Providers { get; set; } = GetValues<DownloadProvider>();

        public DownloadProvider CurrentProvider { get; set; }

        public Radio<DownloadOption> SelectedRadio { get; set; }

        public RelayCommand SearchCommand => new RelayCommand(() => MessengerInstance.Send(SelectedRadio));

        public RelayCommand LogCommand => new RelayCommand(() => MessengerInstance.Send(Component.Log));

        //

        private void OnCurrentProviderChanged()
        {
            if (_settings.Provider == CurrentProvider)
                return;

            _settings.Provider = CurrentProvider;
            _settings.Save();
            ViewModelLocator.RegisterIDownloadService();
        }
    }
}