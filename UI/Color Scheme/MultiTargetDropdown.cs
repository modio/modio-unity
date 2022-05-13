using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

namespace ModIOBrowser
{
    public class MultiTargetDropdown : TMP_Dropdown
    {
        public ColorScheme scheme;

        public List<Target> extraTargets = new List<Target>();

        GameObject border;

        public static MultiTargetDropdown currentMultiTargetDropdown;

        public override void OnSubmit(BaseEventData eventData)
        {
            base.OnSubmit(eventData);
            currentMultiTargetDropdown = this;
        }

        public override void OnCancel(BaseEventData eventData)
        {
            base.OnSubmit(eventData);
            currentMultiTargetDropdown = null;
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            template = transform.Find("Template") as RectTransform;
            Transform label = transform.Find("Label");
            if(label != null)
            {
                captionText = label.GetComponent<TMP_Text>();
            }
            Transform item = transform.Find("Template/Viewport/Content/Item/Item Label");
            if(item != null)
            {
                itemText = item.GetComponent<TMP_Text>();
            }
        }
#endif // UNITY_EDITOR

        protected override TMP_Dropdown.DropdownItem CreateItem(
            TMP_Dropdown.DropdownItem itemTemplate)
        {
            TMP_Dropdown.DropdownItem item = base.CreateItem(itemTemplate);
            if(border == null)
            {
                border = itemTemplate.transform.parent.Find("Border")?.gameObject;
            }
            return item;
        }

        protected override GameObject CreateBlocker(Canvas rootCanvas)
        {
            GameObject item = base.CreateBlocker(rootCanvas);
            if(border != null)
            {
                border.transform.SetAsLastSibling();
            }
            return item;
        }
    }
}
