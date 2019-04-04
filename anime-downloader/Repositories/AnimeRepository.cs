using System;
using System.Collections.Generic;
using System.IO;
using anime_downloader.Classes;
using anime_downloader.Models;
using anime_downloader.Repositories.Interface;
using GalaSoft.MvvmLight;
using Newtonsoft.Json;

namespace anime_downloader.Repositories
{
    [Serializable]
    public class AnimeRepository: ObservableObject, IAnimeRepository
    {
        [JsonIgnore]
        private static string SavePath => Path.Combine(App.Path.Directory.Application, "anime.json");

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
            try
            {
                using (var stream = new StreamWriter(SavePath))
                {
                    var data = JsonConvert.SerializeObject(this, Formatting.Indented,
                        new JsonSerializerSettings {DefaultValueHandling = DefaultValueHandling.Ignore});
                    stream.Write(data);
                }
            }

            catch (Exception e)
            {
                Methods.Alert($"There was an issue saving the anime list:\n{e.Message}");
            }
        }
    }
}