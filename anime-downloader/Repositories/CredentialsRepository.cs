using System;
using System.IO;
using anime_downloader.Classes;
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
        private static string SavePath => Path.Combine(App.Path.Directory.Application, "credentials.json");

        // 

        [JsonProperty("myanimelist")]
        public MyAnimeListConfiguration MyAnimeListConfig { get; set; } = new MyAnimeListConfiguration();

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
                Methods.Alert($"There was an issue saving your credentials:\n{e.Message}");
            }
        }

    }
}