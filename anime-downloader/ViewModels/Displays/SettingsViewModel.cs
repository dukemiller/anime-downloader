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
        public SettingsViewModel(ISettingsRepository settings, ICredentialsRepository credentials)
        {
            Credentials = credentials;
            Settings = settings;
            Subgroups = string.Join(", ", Settings.Subgroups);

            Credentials.MyAnimeListConfig.PropertyChanged += Model_PropertyChanged;
            Settings.FlagConfig.PropertyChanged += Model_PropertyChanged;
            Settings.PathConfig.PropertyChanged += Model_PropertyChanged;
        }

        // 

        public ISettingsRepository Settings { get; set; }

        public ICredentialsRepository Credentials { get; set; }

        public string Subgroups { get; set; }

        public bool ChangeMade { get; set; }

        public RelayCommand SaveCommand => new RelayCommand(Save);

        public RelayCommand TrayToggleCommand => new RelayCommand(() => { });

        // 

        private void Save()
        {
            Settings.Subgroups = Regex.Split(Subgroups, ", ").ToList();
            Settings.Save();
            ChangeMade = false;
        }

        private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e) => ChangeMade = true;
    }
}