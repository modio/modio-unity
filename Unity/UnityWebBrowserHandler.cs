using Modio.Platforms;
using UnityEngine.Device;

namespace Modio.Unity
{
    public class UnityWebBrowserHandler : IWebBrowserHandler
    {
        public void OpenUrl(string url) => Application.OpenURL(url);
    }
}
