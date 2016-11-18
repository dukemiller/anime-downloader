using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using anime_downloader.Classes;
using GalaSoft.MvvmLight;

namespace anime_downloader.ViewModels
{
    class SettingsViewModel: ViewModelBase
    {

        private Settings _settings;

        public SettingsViewModel()
        {
            
        }

        public void New()
        {
            _settings = new Settings();
        }

    }
}
