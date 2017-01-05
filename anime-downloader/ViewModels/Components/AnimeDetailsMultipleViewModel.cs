using System;
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
        private bool _loading;
        private RelayCommand _submitCommand;

        // New
        public AnimeDetailsMultipleViewModel(ISettingsService settings, IAnimeAggregateService animeAggregate)
        {
            Settings = settings;
            AnimeAggregate = animeAggregate;
            Loading = false;
            Header = "Put each anime on own line, each will be added with the template chosen below:";

            Details = new MultipleAnimeDetails
            {
                Resolution = "720",
                Episode = "0",
                Airing = true,
                Status = Status.Considering
            };

            SubmitCommand = new RelayCommand(Create);
        }

        // Load
        public AnimeDetailsMultipleViewModel(ISettingsService settings, IAnimeAggregateService animeAggregate,
            ObservableCollection<Anime> animes)
        {
            Settings = settings;
            AnimeAggregate = animeAggregate;
            Animes = animes;
            Loading = true;
            Header = "Make the same change to the following list of anime: ";

            Input = string.Join("\n", Animes.Select(a => a.Title));
            Details = new MultipleAnimeDetails
            {
                Resolution = Animes.GroupBy(a => a.Resolution).OrderByDescending(c => c.Count()).First().Key,
                Status = Animes.GroupBy(a => a.Status).OrderByDescending(c => c.Count()).First().Key,
                Airing = Animes.GroupBy(a => a.Airing).OrderByDescending(c => c.Count()).First().Key
            };

            SubmitCommand = new RelayCommand(Edit);
        }

        private ObservableCollection<Anime> Animes { get; }
        private ISettingsService Settings { get; }
        private IAnimeAggregateService AnimeAggregate { get; }

        public string Header
        {
            get { return _header; }
            set { Set(() => Header, ref _header, value); }
        }

        public string Input
        {
            get { return _input; }
            set { Set(() => Input, ref _input, value); }
        }

        public MultipleAnimeDetails Details
        {
            get { return _details; }
            set { Set(() => Details, ref _details, value); }
        }

        public bool Loading
        {
            get { return _loading; }
            set { Set(() => Loading, ref _loading, value); }
        }

        public double LoadOpacity => Loading ? 0.6 : 1.0;

        // 

        public RelayCommand SubmitCommand
        {
            get { return _submitCommand; }
            set { Set(() => SubmitCommand, ref _submitCommand, value); }
        }

        private void Create()
        {
            var names = Input
                .Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .Select(n => n.ToLower())
                .ToList();
            if (names.Distinct().Count() != names.Count)
            {
                Methods.Alert("Names have to be unique.");
            }
            else if (AnimeAggregate.AnimeService.Animes.Select(a => a.Name.ToLower()).Intersect(names).Any())
            {
                Methods.Alert("A title entered already exists in the anime list.");
            }
            else
            {
                foreach (var name in names)
                    AnimeAggregate.AnimeService.Add(new Anime
                    {
                        Name = name,
                        Airing = Details.Airing,
                        Episode = int.Parse(Details.Episode),
                        Status = Details.Status,
                        Resolution = Details.Resolution
                    });
                MessengerInstance.Send(Enums.Views.AnimeDisplay);
            }
        }

        private void Edit()
        {
            foreach (var anime in Animes)
            {
                if (!string.IsNullOrEmpty(Details.Rating))
                    anime.Rating = Details.Rating;
                if (!string.IsNullOrEmpty(Details.Episode))
                    anime.Episode = int.Parse(Details.Episode);
                anime.Status = Details.Status;
                anime.Airing = Details.Airing;
                anime.Resolution = Details.Resolution;
            }
            MessengerInstance.Send(Enums.Views.AnimeDisplay);
        }
    }
}