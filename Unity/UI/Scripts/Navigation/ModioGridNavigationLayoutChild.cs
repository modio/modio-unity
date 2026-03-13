using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Navigation
{
    /// <summary>
    /// Used to force ModioGridNavigation to see a layout event, when it's not part of the same layout
    /// (e.g. nested scrollviews)
    /// </summary>
    public class ModioGridNavigationLayoutChild : MonoBehaviour, ILayoutController
    {
        ModioGridNavigation _modioGridNavigation;

        protected void Awake()
        {
            _modioGridNavigation = GetComponentInParent<ModioGridNavigation>();
        }

        public void SetLayoutHorizontal()
        {
        }

        public void SetLayoutVertical()
        {
            if (_modioGridNavigation != null) _modioGridNavigation.SetLayoutVertical();
        }
    }
}
