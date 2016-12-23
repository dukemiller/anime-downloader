using System.ComponentModel;
using anime_downloader.Services;
using anime_downloader.Services.Interfaces;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace anime_downloader.ViewModels
{
    public class SettingsViewModel: ViewModelBase
    {
        private ISettingsService _settings;
        private bool _changeMade;

        public ISettingsService Settings
        {
            get { return _settings; }
            set { Set(() => Settings, ref _settings, value); }
        }

        public SettingsViewModel(ISettingsService settings)
        {
            Settings = settings;

            TrayToggleCommand = new RelayCommand(() => {});
            SaveCommand = new RelayCommand(() =>
            {
                Settings.Save();
                ChangeMade = false;
            });

            Settings.MyAnimeListConfig.PropertyChanged += Model_PropertyChanged;
            Settings.FlagConfig.PropertyChanged += Model_PropertyChanged;
            Settings.PathConfig.PropertyChanged += Model_PropertyChanged;
        }

        private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e) => ChangeMade = true;

        public bool ChangeMade
        {
            get { return _changeMade; }
            set { Set(() => ChangeMade, ref _changeMade, value); }
        }

        public RelayCommand SaveCommand { get; set; }

        public RelayCommand TrayToggleCommand { get; set; }

    }
}
