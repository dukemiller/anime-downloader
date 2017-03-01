using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using anime_downloader.Enums;
using anime_downloader.Models;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace anime_downloader.ViewModels.Components
{
    public class DownloadOptionsViewModel : ViewModelBase
    {
        private static readonly RadioModel<DownloadOption> NextEpisode = new RadioModel<DownloadOption>
        {
            Header = "Next Found Episode",
            Data = DownloadOption.Next
        };

        private static readonly RadioModel<DownloadOption> Continually = new RadioModel<DownloadOption>
        {
            Header = "Continually until no more are found (good for getting up to date)",
            Data = DownloadOption.Continually
        };

        private static readonly RadioModel<DownloadOption> Missing = new RadioModel<DownloadOption>
        {
            Header = "Any missing episodes between first and last episode",
            Data = DownloadOption.Missing
        };

        private ObservableCollection<RadioModel<DownloadOption>> _options;

        private RadioModel<DownloadOption> _selectedRadio;

        // 

        public DownloadOptionsViewModel()
        {
            Options = new ObservableCollection<RadioModel<DownloadOption>> { NextEpisode, Continually, Missing };
            SelectedRadio = Options.First();
            SearchCommand = new RelayCommand(() => MessengerInstance.Send(SelectedRadio));
            LogCommand = new RelayCommand(() => MessengerInstance.Send("download_log"));
        }

        // 

        public ObservableCollection<RadioModel<DownloadOption>> Options
        {
            get { return _options; }
            set { Set(() => Options, ref _options, value); }
        }

        public RadioModel<DownloadOption> SelectedRadio
        {
            get { return _selectedRadio; }
            set { Set(() => SelectedRadio, ref _selectedRadio, value); }
        }

        public RelayCommand SearchCommand { get; set; }

        public RelayCommand LogCommand { get; set; }
    }
}