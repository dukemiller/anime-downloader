using System.Collections.Generic;
using System.Xml.Serialization;

namespace anime_downloader.Models.MyAnimeList
{
    [XmlRoot(ElementName = "myinfo")]
    public class ProfileInfo
    {
        [XmlElement(ElementName = "user_id")]
        public string UserId { get; set; }
        [XmlElement(ElementName = "user_name")]
        public string UserName { get; set; }
        [XmlElement(ElementName = "user_watching")]
        public string UserWatching { get; set; }
        [XmlElement(ElementName = "user_completed")]
        public string UserCompleted { get; set; }
        [XmlElement(ElementName = "user_onhold")]
        public string UserOnhold { get; set; }
        [XmlElement(ElementName = "user_dropped")]
        public string UserDropped { get; set; }
        [XmlElement(ElementName = "user_plantowatch")]
        public string UserPlantowatch { get; set; }
        [XmlElement(ElementName = "user_days_spent_watching")]
        public string UserDaysSpentWatching { get; set; }
    }

    [XmlRoot(ElementName = "anime")]
    public class ProfileAnimeResult
    {
        [XmlElement(ElementName = "series_animedb_id")]
        public string SeriesAnimedbId { get; set; }
        [XmlElement(ElementName = "series_title")]
        public string SeriesTitle { get; set; }
        [XmlElement(ElementName = "series_synonyms")]
        public string SeriesSynonyms { get; set; }
        [XmlElement(ElementName = "series_type")]
        public string SeriesType { get; set; }
        [XmlElement(ElementName = "series_episodes")]
        public string SeriesEpisodes { get; set; }
        [XmlElement(ElementName = "series_status")]
        public string SeriesStatus { get; set; }
        [XmlElement(ElementName = "series_start")]
        public string SeriesStart { get; set; }
        [XmlElement(ElementName = "series_end")]
        public string SeriesEnd { get; set; }
        [XmlElement(ElementName = "series_image")]
        public string SeriesImage { get; set; }

        // 

        [XmlElement(ElementName = "my_id")]
        public string MyId { get; set; }
        [XmlElement(ElementName = "my_watched_episodes")]
        public string MyWatchedEpisodes { get; set; }
        [XmlElement(ElementName = "my_start_date")]
        public string MyStartDate { get; set; }
        [XmlElement(ElementName = "my_finish_date")]
        public string MyFinishDate { get; set; }
        [XmlElement(ElementName = "my_score")]
        public string MyScore { get; set; }
        [XmlElement(ElementName = "my_status")]
        public string MyStatus { get; set; }
        [XmlElement(ElementName = "my_rewatching")]
        public string MyRewatching { get; set; }
        [XmlElement(ElementName = "my_rewatching_ep")]
        public string MyRewatchingEp { get; set; }
        [XmlElement(ElementName = "my_last_updated")]
        public string MyLastUpdated { get; set; }
        [XmlElement(ElementName = "my_tags")]
        public string MyTags { get; set; }
    }

    [XmlRoot(ElementName = "myanimelist")]
    public class ProfileResult
    {
        [XmlElement(ElementName = "myinfo")]
        public ProfileInfo ProfileInfo { get; set; }

        [XmlElement(ElementName = "anime")]
        public List<ProfileAnimeResult> Anime { get; set; }
    }
}
