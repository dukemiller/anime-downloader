using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Services.Interfaces;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace anime_downloader.ViewModels.Components
{
    public class AnimeDetailsMultipleViewModel : ViewModelBase
    {
        private MultipleAnimeDetails _details;
        private string _header;
        private string _input;
        private RelayCommand _submitActionCommand;
        private readonly IAnimeService _animeService;
        private bool _editing;

        // 

        public AnimeDetailsMultipleViewModel(IAnimeService animeService) => _animeService = animeService;

        // 

        public AnimeDetailsMultipleViewModel EditExisting(List<Anime> animes)
        {
            _animes = animes;
            Editing = true;
            Header = "Make the same change to the following list of anime: ";
            Input = string.Join("\n", _animes.Select(a => a.Title));
            Details = new MultipleAnimeDetails
            {
                Resolution = _animes.GroupBy(a => a.Resolution).OrderByDescending(c => c.Count()).First().Key,
                Status = _animes.GroupBy(a => a.Status).OrderByDescending(c => c.Count()).First().Key,
                Airing = _animes.GroupBy(a => a.Airing).OrderByDescending(c => c.Count()).First().Key
            };
            SubmitActionCommand = new RelayCommand(Edit);
            return this;
        }

        public AnimeDetailsMultipleViewModel CreateNew()
        {
            Input = string.Empty;
            Editing = false;
            Header = "Put each anime on own line, each will be added with the template chosen below:";
            Details = new MultipleAnimeDetails
            {
                Resolution = "720",
                Episode = "0",
                Airing = true,
                Status = Status.Considering
            };
            SubmitActionCommand = new RelayCommand(Create);
            return this;
        }

        // 

        public bool Editing
        {
            get => _editing;
            set => Set(() => Editing, ref _editing, value);
        }

        private IList<Anime> _animes;

        public string Header
        {
            get => _header;
            set => Set(() => Header, ref _header, value);
        }

        public string Input
        {
            get => _input;
            set => Set(() => Input, ref _input, value);
        }

        public MultipleAnimeDetails Details
        {
            get => _details;
            set => Set(() => Details, ref _details, value);
        }
        
        public double LoadOpacity => Editing ? 0.6 : 1.0;

        // 

        public RelayCommand SubmitActionCommand
        {
            get => _submitActionCommand;
            set => Set(() => SubmitActionCommand, ref _submitActionCommand, value);
        }

        private void Create()
        {
            var names = Input
                .Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .Select(n => n.ToLower())
                .ToList();

            if (names.Distinct().Count() != names.Count)
                Methods.Alert("Names have to be unique.");

            else if (_animeService.Animes.Select(a => a.Name.ToLower()).Intersect(names).Any())
                Methods.Alert("A title entered already exists in the anime list.");

            else
            {
                foreach (var name in names)
                    _animeService.Add(new Anime
                    {
                        Name = name,
                        Airing = Details.Airing,
                        Episode = int.Parse(Details.Episode),
                        Status = Details.Status,
                        Resolution = Details.Resolution,
                        Details = new AnimeDetails
                        {
                            NeedsUpdating = true
                        }
                    });
                MessengerInstance.Send(Enums.ViewDisplay.Anime);
            }
        }

        private void Edit()
        {
            foreach (var anime in _animes)
            {
                if (!string.IsNullOrEmpty(Details.Rating))
                    anime.Rating = Details.Rating;
                if (!string.IsNullOrEmpty(Details.Episode))
                    anime.Episode = int.Parse(Details.Episode);
                anime.Status = Details.Status;
                anime.Airing = Details.Airing;
                anime.Resolution = Details.Resolution;
            }
            MessengerInstance.Send(Enums.ViewDisplay.Anime);
        }
    }
}