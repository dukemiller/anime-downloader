using System;
using System.Threading.Tasks;
using System.Timers;
using anime_downloader.Repositories.Interface;
using anime_downloader.Services.Interfaces;
using GalaSoft.MvvmLight.Ioc;

namespace anime_downloader.Classes
{
    public static class VersionManager
    {
        private static DateTime _updateCheckDelay = DateTime.Now;

        private static ISettingsRepository SettingsService => SimpleIoc.Default.GetInstance<ISettingsRepository>();

        private static IVersionService VersionService => SimpleIoc.Default.GetInstance<IVersionService>();

        // 

        static VersionManager() => Setup();

        //   

        public static bool NeedsUpdate { get; set; } = SettingsService.Version.NeedsUpdate;

        public static event EventHandler<bool> NeedsUpdateChanged;

        public static string Version => "v" + VersionService.LocalVersion;

        public static async Task Update() => await VersionService.Update();

        public static async Task Check()
        {
            if (DateTime.Now > _updateCheckDelay)
            {
                await VersionService.RefreshVersion();
                CheckForUpdates();
                _updateCheckDelay = DateTime.Now.AddMinutes(20);
            }
        }
        // 

        private static void Setup()
        {
            // We already know it needs an update, dont set any timers
            if (SettingsService.Version.NeedsUpdate)
                return;

            // Check if it needs an update
            if (DateTime.Now >= SettingsService.Version.LastChecked)
            {
                CheckForUpdates();
                SetTimer();
            }

            // Schedule for a future check if inbetween delay
            else
                SetOffsetTimer();
        }

        /**
         * Set a timer from the difference between last checked to now
         * to check for updates and create the timer.
         */
        private static void SetOffsetTimer()
        {
            var timer = new Timer
            {
                Interval = (SettingsService.Version.LastChecked - DateTime.Now).TotalMilliseconds,
                AutoReset = false,
                Enabled = true
            };

            timer.Elapsed += (sender, args) =>
            {
                CheckForUpdates();
                SetTimer();
            };
        }

        /**
         * Create a timer to check every six hours.
         */
        private static void SetTimer()
        {
            var timer = new Timer
            {
                Interval = new TimeSpan(6, 0, 0).TotalMilliseconds,
                AutoReset = true,
                Enabled = true
            };

            timer.Elapsed += (sender, args) => CheckForUpdates();
        }

        /**
         * Fetch information from service and update appropriately.
         */
        private static async void CheckForUpdates()
        {
            // Retrieve if there needs to be an update
            var update = await VersionService.NeedsUpdate();
            if (update != NeedsUpdate)
                NeedsUpdateChanged?.Invoke(null, update);
            NeedsUpdate = update;
            SettingsService.Version.NeedsUpdate = update;

            // If there's no update, set minimum threshold to check again in 20 minutes
            // (though it continues previous timer of 6 hours)
            if (!NeedsUpdate)
            {
                SettingsService.Version.LastChecked = DateTime.Now.AddMinutes(20);
                SettingsService.Save();
            }
        }

    }
}
