using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Repositories.Interface;
using anime_downloader.Services.Interfaces;
using GalaSoft.MvvmLight;

namespace anime_downloader.ViewModels.Components.Download
{
    public class OutputViewModel : ViewModelBase
    {
        private readonly ISettingsRepository _settings;

        private readonly IDownloadService _downloadService;

        // 

        public OutputViewModel(ISettingsRepository settings, IDownloadService downloadService)
        {
            _settings = settings;
            _downloadService = downloadService;
        }

        // 

        public string Text { get; set; }

        public async Task Download(Radio<DownloadOption> radio)
        {
            Text = "";

            if (!await _settings.CrucialDirectoriesExist())
            {
                Text = ">> Not all paths have been correctly configured.";
                return;
            }

            MessengerInstance.Send(ViewState.IsWorking);

            if (await _downloadService.Available())
                try
                {
                    switch (radio.Data)
                    {
                        case DownloadOption.Next:
                            Text = ">> Searching for currently airing anime episodes ...\n";
                            break;

                        case DownloadOption.Continually:
                            if (!await Methods.QuestionYesNo(
                                "You could potentially download an entire wrong series if\n" +
                                "the intended series isn't found by your anime name and \n" +
                                "settings. Be sure everything on your list retrieves the\n" +
                                "show you intend.\n\n" +
                                "Are you sure you want to continue?"))
                            {
                                MessengerInstance.Send(ViewState.DoneWorking);
                                MessengerInstance.Send(Display.Download);
                                return;
                            }

                            Text = ">> Attempting to catch up on airing anime episodes ...\n";
                            break;

                        case DownloadOption.Missing:
                            Text = ">> Finding all missing episodes ...\n";
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    var downloaded = await _downloadService.Download(radio.Data, Append);

                    Text += downloaded > 0
                        ? $">> Found {downloaded} anime download{(downloaded == 1 ? "" : "s")}."
                        : ">> No new anime found.";
                }

                catch (Exception exception)
                {
                    if (exception is WebException webException
                        && webException.Status == WebExceptionStatus.ProtocolError
                        && Regex.IsMatch(webException.Message, @"\(5\d{2}\)"))
                        Text += ">> The server returned an internal error. Try again in a bit.";

                    else
                        Text += ">> An error occured while attempting to download, try again.";
                }

            else
                Text += $">> {_downloadService.Name} is currently offline. Try checking later.";

            MessengerInstance.Send(ViewState.DoneWorking);
        }

        public async Task Log()
        {
            Text = "";

            if (File.Exists(App.Path.Logging))
                using (var reader = new StreamReader(App.Path.Logging))
                {
                    var data = await reader.ReadToEndAsync();
                    Text = await Task.Run(() => string.Join("\n", data.Split('\n').Reverse().Skip(1)));
                }

            else
                Text = ">> No downloads have been logged so far.";
        }

        // 

        private void Append(string text) => Text += text + "\n";
    }
}