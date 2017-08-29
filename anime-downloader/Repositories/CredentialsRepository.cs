using System;
using System.IO;
using anime_downloader.Models.Configurations;
using anime_downloader.Repositories.Interface;
using GalaSoft.MvvmLight;
using Newtonsoft.Json;

namespace anime_downloader.Repositories
{
    [Serializable]
    public class CredentialsRepository: ObservableObject, ICredentialsRepository
    {
        [JsonIgnore]
        private static string SavePath => Path.Combine(PathConfiguration.ApplicationDirectory, "credentials.json");

        // 

        [JsonProperty("myanimelist")]
        public MyAnimeListConfiguration MyAnimeListConfig { get; set; } = new MyAnimeListConfiguration();

        [JsonProperty("anilist")]
        public AniListConfiguration AniListConfiguration { get; set; } = new AniListConfiguration();

        // 

        public static CredentialsRepository Load()
        {
            if (File.Exists(SavePath))
                using (var stream = new StreamReader(SavePath))
                    return JsonConvert.DeserializeObject<CredentialsRepository>(stream.ReadToEnd());

            return new CredentialsRepository();
        }

        public void Save()
        {
            using (var stream = new StreamWriter(SavePath))
                stream.Write(JsonConvert.SerializeObject(this, Formatting.Indented));
        }

    }
}