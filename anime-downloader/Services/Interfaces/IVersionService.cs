using System.Threading.Tasks;
using anime_downloader.Models;

namespace anime_downloader.Services.Interfaces
{
    public interface IVersionService
    {
        Task<bool> NeedsUpdate();

        Task<SemanticVersion> RefreshVersion();

        Task<SemanticVersion> OnlineVersion { get; }

        SemanticVersion LocalVersion { get; }

        Task Update();
    }
}