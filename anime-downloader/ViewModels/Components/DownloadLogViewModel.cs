using System.IO;
using System.Linq;
using System.Threading.Tasks;
using anime_downloader.Repositories.Interface;
using anime_downloader.Services.Interfaces;
using GalaSoft.MvvmLight;

namespace anime_downloader.ViewModels.Components
{
    public class DownloadLogViewModel : ViewModelBase
    {
        private string _text;

        private readonly ISettingsRepository _settings;

        public DownloadLogViewModel(ISettingsRepository settings)
        {
            _settings = settings;
        }

        public string Text
        {
            get => _text;
            set => Set(() => Text, ref _text, value);
        }

        public async void DisplayLogResults()
        {
            Text = "";
            if (File.Exists(_settings.PathConfig.Logging))
                using (var reader = new StreamReader(_settings.PathConfig.Logging))
                {
                    var data = await reader.ReadToEndAsync();
                    Text = await Task.Run(() => string.Join("\n", data.Split('\n').Reverse().Skip(1)));
                }
            else
                Text = ">> No downloads have been logged so far.";
        }
    }
}