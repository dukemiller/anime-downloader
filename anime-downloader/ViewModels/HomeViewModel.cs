using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using anime_downloader.Classes;
using anime_downloader.Models;
using anime_downloader.Services.Interfaces;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace anime_downloader.ViewModels
{
    public class HomeViewModel : ViewModelBase
    {
        private readonly IVersionService _versionService;

        private string _version;

        private bool _needsUpdate = true;

        public HomeViewModel(IVersionService versionService)
        {
            _versionService = versionService;
            
            UpdateCommand = new RelayCommand(Update);
            Version = Assembly.GetExecutingAssembly()
                .GetName()
                .Version
                .ToString();
            CheckForUpdates();
            StartTimer();
        }

        private async void CheckForUpdates() => NeedsUpdate = await _versionService.NeedsUpdate();

        public bool NeedsUpdate
        {
            get { return _needsUpdate; }
            set { Set(() => NeedsUpdate, ref _needsUpdate, value); }
        }

        public string Version
        {
            get { return _version; }
            set { Set(() => Version, ref _version, value); }
        }

        // 

        public RelayCommand UpdateCommand { get; set; }

        // 

        private void StartTimer()
        {
            var timer = new System.Timers.Timer
            {
                Interval = new TimeSpan(1, 0, 0).TotalMilliseconds,
                AutoReset = true,
                Enabled = true
            };
            timer.Elapsed += (sender, args) => CheckForUpdates();
        }

        private async void Update()
        {
            MessengerInstance.Send(new WorkMessage { Working = true });
            await _versionService.Update();
        }
    }
}