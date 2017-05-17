using System;
using System.ComponentModel;
using anime_downloader.Classes;

namespace anime_downloader.Enums
{
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    [Serializable]
    public enum DownloadProvider
    {
        [Description("Nyaa.pantsu.cat")]
        NyaaPantsu,

        [Description("Nyaa.si")]
        NyaaSi,

        [Description("HorribleSubs.info")]
        HorribleSubs
    }
}