using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
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
        private string _subgroups;

        public ISettingsService Settings
        {
            get { return _settings; }
            set { Set(() => Settings, ref _settings, value); }
        }

        public string Subgroups
        {
            get { return _subgroups; }
            set { Set(() => Subgroups, ref _subgroups, value); }
        }

        public SettingsViewModel(ISettingsService settings)
        {
            Settings = settings;
            
            Subgroups = string.Join(", ", Settings.Subgroups);

            TrayToggleCommand = new RelayCommand(() => {});
            SaveCommand = new RelayCommand(() =>
            {
                Settings.Subgroups = Regex.Split(Subgroups, ", ").ToList();
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
