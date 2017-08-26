using Newtonsoft.Json;

namespace anime_downloader.Models.MyAnimeList
{
    public class Payload
    {
        [JsonProperty("media_type")]
        public string MediaType { get; set; }

        [JsonProperty("start_year")]
        public int StartYear { get; set; }

        [JsonProperty("aired")]
        public string Aired { get; set; }

        [JsonProperty("score")]
        public string Score { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }
}