using System.ComponentModel;
using anime_downloader.Models.Configurations;

namespace anime_downloader.Repositories.Interface
{
    public interface ICredentialsRepository: INotifyPropertyChanged
    {
        MyAnimeListConfiguration MyAnimeListConfig { get; set; }
        AniListConfiguration AniListConfiguration { get; set; }
        void Save();
    }
}