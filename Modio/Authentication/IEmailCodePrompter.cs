
using System.Threading.Tasks;

namespace Modio.Authentication
{

    public interface IEmailCodePrompter
    {
        Task<string> ShowCodePrompt();
    }
}
