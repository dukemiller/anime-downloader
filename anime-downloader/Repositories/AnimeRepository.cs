using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using anime_downloader.Models;
using anime_downloader.Models.Configurations;
using anime_downloader.Repositories.Interface;
using Newtonsoft.Json;

namespace anime_downloader.Repositories
{
    [Serializable]
    public class AnimeRepository: IAnimeRepository
    {
        [JsonIgnore]
        private static string SavePath => Path.Combine(PathConfiguration.ApplicationDirectory, "anime.json");

        [JsonProperty("anime")]
        public List<Anime> Animes { get; set; } = new List<Anime>();

        // 

        public static AnimeRepository Load()
        {
            if (File.Exists(SavePath))
                using (var stream = new StreamReader(SavePath))
                    return JsonConvert.DeserializeObject<AnimeRepository>(stream.ReadToEnd());

            return new AnimeRepository();
        }

        public void Save()
        {
            using (var stream = new StreamWriter(SavePath))
                stream.Write(JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }
}