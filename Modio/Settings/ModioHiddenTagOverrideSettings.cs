using System;

namespace Modio.Settings
{
    [Serializable]
    public class ModioHiddenTagOverrideSettings : IModioServiceSettings
    {
        public string[] HideTagCategories;
    }
}
