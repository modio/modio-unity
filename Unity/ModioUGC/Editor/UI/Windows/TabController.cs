using System;
using UnityEngine.UIElements;

namespace Modio.ModioUGC
{
    public class TabController
    {
        readonly Tab _tab;

        internal Tab Tab => _tab;

        internal TabController(Tab tab) => _tab = tab;

        internal TabController(VisualElement root, string selector) => _tab = root.Q<Tab>(selector);


        internal void SetTabVisibility(bool visible)
        {
            if (_tab == null)
                throw new InvalidOperationException("Tab is null");

            _tab.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            _tab.tabHeader.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        internal void Show()
        {
            if (_tab == null)
                throw new InvalidOperationException("Tab is null");

            _tab.style.display = DisplayStyle.Flex;
            _tab.tabHeader.style.display = DisplayStyle.Flex;
        }

        internal void Hide()
        {
            if (_tab == null)
                throw new InvalidOperationException("Tab is null");

            _tab.style.display = DisplayStyle.None;
            _tab.tabHeader.style.display = DisplayStyle.None;
        }
    }
}
