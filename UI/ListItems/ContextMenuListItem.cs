using System;
using TMPro;
using UnityEngine;

namespace ModIOBrowser.Implementation
{
    /// <summary>
    /// This is used for each button added to the context menu when it is opened.
    /// </summary>
    internal class ContextMenuListItem : ListItem
    {
        [SerializeField] TMP_Text optionText;
        [SerializeField] MultiTargetButton optionButton;

        public override void Select()
        {
            optionButton.Select();
        }

        public override void Setup(string title, Action onClick)
        {
            base.Setup(title);
            optionText.text = title;
            optionButton.onClick.RemoveAllListeners();
            optionButton.onClick.AddListener(delegate { onClick(); });
            gameObject.SetActive(true);
            optionButton.enabled = true;
        }
    }
}
