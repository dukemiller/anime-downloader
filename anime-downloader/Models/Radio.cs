using GalaSoft.MvvmLight;

namespace anime_downloader.Models
{
    public class Radio<T> : ObservableObject
    {
        public string Header { get; set; } = "";

        public string ToolTip { get; set; } = "";

        public T Data { get; set; }
    }

    public abstract class Radio: Radio<string> { }
}