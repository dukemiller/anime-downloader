﻿using GalaSoft.MvvmLight;

namespace anime_downloader.Models
{
    public class RadioModel<T> : ViewModelBase
    {
        private T _data;

        private string _header = string.Empty;

        private string _tooltip = string.Empty;

        public string Header
        {
            get { return _header; }
            set { Set(() => Header, ref _header, value); }
        }

        public string ToolTip
        {
            get { return _tooltip; }
            set { Set(() => ToolTip, ref _tooltip, value); }
        }

        public T Data
        {
            get { return _data; }
            set { Set(() => Data, ref _data, value); }
        }
    }
}