using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using anime_downloader.Repositories.Interface;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace anime_downloader.ViewModels.Displays
{
    public class SettingsViewModel : ViewModelBase
    {
        private bool _changeMade;
        private ISettingsRepository _settings;
        private ICredentialsRepository _credentials;
        private string _subgroups;

        public SettingsViewModel(ISettingsRepository settings, ICredentialsRepository credentials)
        {
            _credentials = credentials;
            Settings = settings;
            Subgroups = string.Join(", ", Settings.Subgroups);

            TrayToggleCommand = new RelayCommand(() => { });
            SaveCommand = new RelayCommand(() =>
            {
                Settings.Subgroups = Regex.Split(Subgroups, ", ").ToList();
                Settings.Save();
                ChangeMade = false;
            });

            Credentials.MyAnimeListConfig.PropertyChanged += Model_PropertyChanged;
            Credentials.AniListConfiguration.PropertyChanged += Model_PropertyChanged;
            Settings.FlagConfig.PropertyChanged += Model_PropertyChanged;
            Settings.PathConfig.PropertyChanged += Model_PropertyChanged;
        }
        
        public ISettingsRepository Settings
        {
            get => _settings;
            set => Set(() => Settings, ref _settings, value);
        }

        public ICredentialsRepository Credentials
        {
            get => _credentials;
            set => Set(() => Credentials, ref _credentials, value);
        }

        public string Subgroups
        {
            get => _subgroups;
            set => Set(() => Subgroups, ref _subgroups, value);
        }

        public bool ChangeMade
        {
            get => _changeMade;
            set => Set(() => ChangeMade, ref _changeMade, value);
        }

        public RelayCommand SaveCommand { get; set; }

        public RelayCommand TrayToggleCommand { get; set; }

        private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e) => ChangeMade = true;
    }
}