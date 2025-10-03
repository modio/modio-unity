using System.Threading.Tasks;
using Modio.Authentication;

namespace Modio.Images
{
    public interface IExternalAvatarProviderService<TImage>
    {
        Task<(Error error, TImage image)> TryGetAvatarImage();
    }
}
