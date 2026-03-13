using System;
using System.Collections;
using System.Collections.Generic;
using Modio.Unity.UI.Components.Selectables;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Modio.Unity.UI.Components
{
    [RequireComponent(typeof(ModioUIButton))]
    public class ModioUITermsLinkButton : MonoBehaviour
    {
        [SerializeField] TMP_Text _text;
        [SerializeField] ModioUIButton _button;
        LinkType _linkType;
        
        public void SetupButton(string text, LinkType linkType, UnityAction hyperlinkAction)
        {
            _text.text = text;
            _linkType = linkType;
            
            if(_button == null)
                _button = GetComponent<ModioUIButton>();
            
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(hyperlinkAction);
        }
    }
}
