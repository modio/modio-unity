using Modio.Unity.UI.Panels;
using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Input
{
    public class ModioUITabNavigationToggleGroup : ToggleGroup
    {
        [SerializeField] ModioUIInput.ModioAction _leftAction = ModioUIInput.ModioAction.TabLeft;
        [SerializeField] ModioUIInput.ModioAction _rightAction = ModioUIInput.ModioAction.TabRight;

        [SerializeField] bool _loopSelection;

        ModioPanelBase _parentPanel;

        protected override void Awake()
        {
            base.Awake();
            _parentPanel = GetComponentInParent<ModioPanelBase>();
        }

        protected override void OnEnable()
        {
            if (_parentPanel != null)
            {
                _parentPanel.OnHasFocusChanged += OnPanelChangedFocus;
                if (_parentPanel.HasFocus) OnPanelChangedFocus(true);
            }
            else
            {
                OnPanelChangedFocus(true);
            }

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            if (_parentPanel != null)
            {
                _parentPanel.OnHasFocusChanged -= OnPanelChangedFocus;
            }
            OnPanelChangedFocus(false);

            base.OnDisable();
        }

        void OnPanelChangedFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                ModioUIInput.AddHandler(_leftAction,  TabLeft);
                ModioUIInput.AddHandler(_rightAction, TabRight);
            }
            else
            {
                ModioUIInput.RemoveHandler(_leftAction,  TabLeft);
                ModioUIInput.RemoveHandler(_rightAction, TabRight);
            }
        }

        void TabLeft()
        {
            m_Toggles[ClampIndex(IsOnIndex() - 1)].isOn = true;
        }

        void TabRight()
        {
            m_Toggles[ClampIndex(IsOnIndex() + 1)].isOn = true;
        }

        int ClampIndex(int newIndex)
        {
            if (_loopSelection) return (newIndex + m_Toggles.Count) % m_Toggles.Count;

            return Mathf.Clamp(newIndex, 0, m_Toggles.Count - 1);
        }

        int IsOnIndex()
        {
            m_Toggles.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

            for (var i = 0; i < m_Toggles.Count; i++)
            {
                if (m_Toggles[i].isOn) return i;
            }

            return 0;
        }
    }
}
