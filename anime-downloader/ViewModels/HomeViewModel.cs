using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using anime_downloader.Classes;
using anime_downloader.Models;
using anime_downloader.Services.Interfaces;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;

namespace anime_downloader.ViewModels
{
    public class HomeViewModel : ViewModelBase
    {
        private readonly ISettingsService _settingsService;

        private readonly IVersionService _versionService;

        private string _version;

        private bool _needsUpdate;

        private DateTime _updateCheckDelay = DateTime.Now;

        public HomeViewModel(ISettingsService settingsService, IVersionService versionService)
        {
            _settingsService = settingsService;
            _versionService = versionService;
            
            UpdateCommand = new RelayCommand(Update);
            Version = Assembly.GetExecutingAssembly()
                .GetName()
                .Version
                .ToString();

            // The double call to this is necessary, this function can be called
            // in multiple areas
            if (DateTime.Now >= _settingsService.UpdateCheckDelay)
                CheckForUpdates();
            else
                DelayedCheckForUpdates();

            SetRepeatingUpdateTimer();
            MessengerInstance.Register<NotificationMessage>(this, async msg =>
            {
                if (msg.Notification.Equals("check_for_updates") 
                    && DateTime.Now > _updateCheckDelay
                    && DateTime.Now > _settingsService.UpdateCheckDelay)
                {
                    await _versionService.RefreshVersion();
                    CheckForUpdates();
                    _updateCheckDelay = DateTime.Now.AddMinutes(20);
                }
            });
        }

        private async void CheckForUpdates()
        {
            NeedsUpdate = await _versionService.NeedsUpdate();
            _settingsService.UpdateCheckDelay = DateTime.Now.AddMinutes(20);
            _settingsService.Save();
        }

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

        private void DelayedCheckForUpdates()
        {
            var timer = new System.Timers.Timer
            {
                Interval = (_settingsService.UpdateCheckDelay - DateTime.Now).TotalMilliseconds,
                AutoReset = false,
                Enabled = true
            };
            timer.Elapsed += (sender, args) => CheckForUpdates();
        }

        private void SetRepeatingUpdateTimer()
        {
            var timer = new System.Timers.Timer
            {
                Interval = new TimeSpan(6, 0, 0).TotalMilliseconds,
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