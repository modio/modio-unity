using System;
using Modio.Unity.UI.Search;

namespace Modio.Unity.UI.Components
{
    [Serializable]
    public class ModioUISearchCategoryTabOverrideSettings : IModioServiceSettings
    {
        public ModioUISearchSettings[] OverrideMainSearchesTo;
    }
}
