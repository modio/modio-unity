using System;

namespace Modio
{
    public class ModioDebugMenuAttribute : Attribute
    {
        public bool ShowInSettingsMenu = true;
        public bool ShowInBrowserMenu = true;
    }
}
