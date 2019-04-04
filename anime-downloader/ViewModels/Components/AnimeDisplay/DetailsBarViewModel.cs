using System.Diagnostics;
using System.Windows;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Repositories.Interface;
using anime_downloader.Services.Interfaces;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using PropertyChanged;

namespace anime_downloader.ViewModels.Components.AnimeDisplay
{
    public class DetailsBarViewModel : ViewModelBase
    {
        private readonly IDetailProviderService _detailService;

        private readonly IAnimeRepository _animeRepository;

        // 

        public DetailsBarViewModel(IDetailProviderService detailService, IAnimeRepository animeRepository)
        {
            _detailService = detailService;
            _animeRepository = animeRepository;
        }

        public DetailsBarViewModel Load(Anime anime)
        {
            Anime = anime;
            return this;
        }

        // 

        public Anime Anime { get; set; }
        
        public RelayCommand ProfileCommand => new RelayCommand(Profile);

        public RelayCommand RefreshCommand => new RelayCommand(Refresh);

        [DependsOn(nameof(Anime))]
        public Visibility MalVisibility => Anime.Details.HasId ? Visibility.Visible : Visibility.Collapsed;
        
        // 

        private void Profile()
        {
            Process.Start($"http://myanimelist.net/anime/{Anime.Details.Id}");
        }

        private async void Refresh()
        {
            MessengerInstance.Send(ViewState.IsWorking);

            var (successful, changesMade) = await _detailService.FillInDetails(Anime);

            if (changesMade)
                _animeRepository.Save();

            if (!successful)
                Methods.Alert("Had trouble finding details about this show.");

            MessengerInstance.Send(ViewState.DoneWorking);
        }
    }
}