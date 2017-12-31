using System.Collections.ObjectModel;
using System.Linq;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Repositories.Interface;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace anime_downloader.ViewModels.Components.Download
{
    public class OptionsViewModel : ViewModelBase
    {
        private readonly ISettingsRepository _settings;

        private static readonly RadioModel<DownloadOption> NextEpisode = new RadioModel<DownloadOption>
        {
            Header = "Search for next found episode",
            Data = DownloadOption.Next
        };

        private static readonly RadioModel<DownloadOption> Continually = new RadioModel<DownloadOption>
        {
            Header = "Continually search until no more are found (good for getting up to date)",
            Data = DownloadOption.Continually
        };

        private static readonly RadioModel<DownloadOption> Missing = new RadioModel<DownloadOption>
        {
            Header = "Download any missing episodes between first and last downloaded episode",
            Data = DownloadOption.Missing
        };

        private ObservableCollection<RadioModel<DownloadOption>> _options;

        private RadioModel<DownloadOption> _selectedRadio;

        private DownloadProvider _currentProvider;

        // 

        public OptionsViewModel(ISettingsRepository settings)
        {
            _settings = settings;
            _currentProvider = _settings.Provider;
            Options = new ObservableCollection<RadioModel<DownloadOption>> { NextEpisode, Continually, Missing };
            SelectedRadio = Options.First();
            SearchCommand = new RelayCommand(() => MessengerInstance.Send(SelectedRadio));
            LogCommand = new RelayCommand(() => MessengerInstance.Send("download_log"));
        }

        // 

        public ObservableCollection<RadioModel<DownloadOption>> Options
        {
            get => _options;
            set => Set(() => Options, ref _options, value);
        }

        public ObservableCollection<DownloadProvider> Providers { get; set; } =
            new ObservableCollection<DownloadProvider>(Classes.Extensions.GetValues<DownloadProvider>());

        public DownloadProvider CurrentProvider
        {
            get => _currentProvider;
            set
            {
                Set(() => CurrentProvider, ref _currentProvider, value);
                _settings.Provider = value;
                _settings.Save();
                ViewModelLocator.RegisterIDownloadService();
            }
        }

        public RadioModel<DownloadOption> SelectedRadio
        {
            get => _selectedRadio;
            set => Set(() => SelectedRadio, ref _selectedRadio, value);
        }

        public RelayCommand SearchCommand { get; set; }

        public RelayCommand LogCommand { get; set; }
    }
}