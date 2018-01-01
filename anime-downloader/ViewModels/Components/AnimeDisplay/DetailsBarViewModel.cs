using System.Diagnostics;
using System.Windows;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Repositories.Interface;
using anime_downloader.Services.Interfaces;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace anime_downloader.ViewModels.Components.AnimeDisplay
{
    public class DetailsBarViewModel : ViewModelBase
    {
        private Anime _anime;

        private readonly IDetailProviderService _detailService;

        private readonly IAnimeRepository _animeRepository;

        // 

        public DetailsBarViewModel(IDetailProviderService detailService, IAnimeRepository animeRepository)
        {
            _detailService = detailService;
            _animeRepository = animeRepository;
            ProfileCommand = new RelayCommand(Profile);
            RefreshCommand = new RelayCommand(Refresh);
        }

        public DetailsBarViewModel Load(Anime anime)
        {
            _anime = anime;
            RaisePropertyChanged(nameof(MalVisibility));
            return this;
        }

        // 
        
        public RelayCommand ProfileCommand { get; set; }

        public RelayCommand RefreshCommand { get; set; }

        public Visibility MalVisibility => _anime.Details.HasId ? Visibility.Visible : Visibility.Collapsed;
        
        // 

        private void Profile()
        {
            Process.Start($"http://myanimelist.net/anime/{_anime.Details.Id}");
        }

        private async void Refresh()
        {
            MessengerInstance.Send(ViewState.IsWorking);

            var (successful, changesMade) = await _detailService.FillInDetails(_anime);

            if (changesMade)
                _animeRepository.Save();

            if (!successful)
                Methods.Alert("Had trouble finding details about this show.");

            MessengerInstance.Send(ViewState.DoneWorking);
        }
    }
}