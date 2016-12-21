using System.Collections.ObjectModel;
using System.Linq;
using anime_downloader.Models;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace anime_downloader.ViewModels.Components
{
    public class DownloadOptionsViewModel : ViewModelBase
    {
        private static readonly RadioModel NextEpisode = new RadioModel
        {
            Header = "Next Found Episode",
            Tag = "Next"
        };

        private static readonly RadioModel Continually = new RadioModel
        {
            Header = "Continually until no more are found (good for getting up to date)",
            Tag = "Continually"
        };

        private static readonly RadioModel Missing = new RadioModel
        {
            Header = "Any missing episodes between first and last episode",
            Tag = "Missing"
        };

        private ObservableCollection<RadioModel> _options;

        private RadioModel _selectedRadio;

        // 

        public DownloadOptionsViewModel()
        {
            Options = new ObservableCollection<RadioModel> {NextEpisode, Continually, Missing};
            SelectedRadio = Options.First();
            SearchCommand = new RelayCommand(() => MessengerInstance.Send(SelectedRadio));
            LogCommand = new RelayCommand(() => MessengerInstance.Send("download_log"));
        }

        // 

        public ObservableCollection<RadioModel> Options
        {
            get { return _options; }
            set { Set(() => Options, ref _options, value); }
        }

        public RadioModel SelectedRadio
        {
            get { return _selectedRadio; }
            set { Set(() => SelectedRadio, ref _selectedRadio, value); }
        }

        public RelayCommand SearchCommand { get; set; }

        public RelayCommand LogCommand { get; set; }
    }
}