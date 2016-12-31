using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace anime_downloader.Enums
{
    [Serializable]
    public enum Status
    {
        [Description("Watching")]
        Watching,

        [Description("Considering")]
        Considering,

        [Description("Finished")]
        Finished,

        [Description("On Hold")]
        OnHold,

        [Description("Dropped")]
        Dropped
    }
}
