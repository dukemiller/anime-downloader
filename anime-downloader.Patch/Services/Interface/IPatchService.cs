using System;

namespace anime_downloader.Patch.Services.Interface
{
    /// <summary>
    ///     Handle all version updates.
    /// </summary>
    public interface IPatchService
    {
        /// <summary>
        ///     The output stream.
        /// </summary>
        Action<string> Output { get; set; }

        /// <summary>
        ///     Make any necessary changes to successfuly go from previous -> current.
        /// </summary>
        (bool updated, bool failed) Patch(Version previous, Version current);
    }
}