using System.Threading.Tasks;
using anime_downloader.Models;

namespace anime_downloader.Services.Interfaces
{
    /// <summary>
    ///     The handler of version control and updating
    /// </summary>
    public interface IVersionService
    {
        /// <summary>
        ///     Check the difference between {LocalVersion} and {OnlineVersion}.
        /// </summary>
        Task<bool> NeedsUpdate();

        /// <summary>
        ///     Refresh the {OnlineVersion} property.
        /// </summary>
        /// <returns></returns>
        Task<SemanticVersion> RefreshVersion();

        /// <summary>
        ///     Version of the remote executable.
        /// </summary>
        Task<SemanticVersion> OnlineVersion { get; }

        /// <summary>
        ///     Version of the currently running executable.
        /// </summary>
        SemanticVersion LocalVersion { get; }

        /// <summary>
        ///     Update from {LocalVersion} to {OnlineVersion}.
        /// </summary>
        Task Update();
    }
}