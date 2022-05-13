using UnityEngine;
using UnityEngine.UI;

namespace ModIOBrowser
{
    public class ColorScheme : MonoBehaviour
    {
#region Internal values
        public Color Dark1;
        public Color Dark2;
        public Color Dark3;
        public Color White;

        internal int H1_px = 48;
        internal int H2_px = 40;
        internal int H3_px = 32;
        internal int H4_px = 24;
        internal int H5_px = 18;
        internal int ParagraphBig_px = 24;
        internal int ParagraphNormal_px = 18;
        internal int SmallTextRegular_px = 16;
        internal int SmallTextSemibold_px = 16;
        internal int MainNavigation_px = 20;

        // internal int BorderThicknessBig_px = 6;
        // internal int BorderThicknessSmall_px = 3;

        // internal int CornerRadiusLarge_px = 40;
        // internal int CornerRadiusStandard_px = 24;
        // internal int CornerRadiusCollection_px = 16;
        // internal int CornerRadiusTags_px = 8;
        // internal int CornerRadiusCheckboxes_px = 4;
#endregion // Internal values

        // Editable colors
        public Color Accent;
        public Color LightGrey1;
        public Color LightGrey2;
        public Color LightGrey3;
        public Color Green;
        public Color Red;
        
        // For setting the light or dark mode of the UI
        public bool LightMode;

        void Reset()
        {
            SetColorsToDefault();
        }

        [ContextMenu("Restore Default Colors")]
        public void SetColorsToDefault()
        {
            ColorUtility.TryParseHtmlString("#1B2038", out Dark1);
            ColorUtility.TryParseHtmlString("#212945", out Dark2);
            ColorUtility.TryParseHtmlString("#0E101B", out Dark3);
            ColorUtility.TryParseHtmlString("#FFFFFF", out White);
            
            ColorUtility.TryParseHtmlString("#07C1D8", out Accent);
            ColorUtility.TryParseHtmlString("#C1C4D7", out LightGrey1);
            ColorUtility.TryParseHtmlString("#DADCE7", out LightGrey2);
            ColorUtility.TryParseHtmlString("#737684", out LightGrey3);
            ColorUtility.TryParseHtmlString("#7EEF8C", out Green);
            ColorUtility.TryParseHtmlString("#DB5355", out Red);
        }
        
        public void RefreshUI()
        {
            ColorSetter[] setters = GetComponentsInChildren<ColorSetter>(true);
            foreach(ColorSetter setter in setters)
            {
                setter.Refresh(this);
            }
        }

        public ColorBlock GetColorBlock_Button()
        {
            ColorBlock colors = new ColorBlock();
            colors.fadeDuration = 0.1f;
            colors.normalColor = LightGrey2;
            colors.highlightedColor = Accent;
            colors.pressedColor = LightGrey2;
            colors.disabledColor = Dark3;
            colors.colorMultiplier = 1;
            return colors;
        }

        public ColorBlock GetColorBlock_Dropdown()
        {
            ColorBlock colors = new ColorBlock();
            colors.normalColor = LightGrey2;
            colors.highlightedColor = Accent;
            colors.pressedColor = LightGrey2;
            colors.disabledColor = Dark3;
            return colors;
        }

        public Color GetSchemeColor(ColorSetterType enumType)
        {
            switch(enumType)
            {
                case ColorSetterType.Dark1:
                    return Dark1;
                case ColorSetterType.Dark2:
                    return Dark2;
                case ColorSetterType.Dark3:
                    return Dark3;
                case ColorSetterType.White:
                    return White;
                case ColorSetterType.Accent:
                    return Accent;
                case ColorSetterType.LightGrey1:
                    return LightGrey1;
                case ColorSetterType.LightGrey2:
                    return LightGrey2;
                case ColorSetterType.LightGrey3:
                    return LightGrey3;
                case ColorSetterType.Green:
                    return Green;
                case ColorSetterType.Red:
                    return Red;
            }
            return default;
        }
    }
}
