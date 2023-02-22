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
        public Color Highlight;
        public Color Inactive1;
        public Color Inactive2;
        public Color Inactive3;
        public Color PositiveAccent;
        public Color NegativeAccent;
        
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
            
            ColorUtility.TryParseHtmlString("#07C1D8", out Highlight);
            ColorUtility.TryParseHtmlString("#C1C4D7", out Inactive1);
            ColorUtility.TryParseHtmlString("#AEB1C2", out Inactive2);
            ColorUtility.TryParseHtmlString("#737684", out Inactive3);
            ColorUtility.TryParseHtmlString("#7EEF8C", out PositiveAccent);
            ColorUtility.TryParseHtmlString("#DB5355", out NegativeAccent);
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
            colors.normalColor = Inactive2;
            colors.highlightedColor = Highlight;
            colors.pressedColor = Inactive2;
            colors.disabledColor = Dark3;
            colors.colorMultiplier = 1;
            return colors;
        }

        public ColorBlock GetColorBlock_Dropdown()
        {
            ColorBlock colors = new ColorBlock();
            colors.normalColor = Inactive2;
            colors.highlightedColor = Highlight;
            colors.pressedColor = Inactive2;
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
                case ColorSetterType.Highlight:
                    return Highlight;
                case ColorSetterType.Inactive1:
                    return Inactive1;
                case ColorSetterType.Inactive2:
                    return Inactive2;
                case ColorSetterType.Inactive3:
                    return Inactive3;
                case ColorSetterType.PositiveAccent:
                    return PositiveAccent;
                case ColorSetterType.NegativeAccent:
                    return NegativeAccent;
            }
            return default;
        }
    }
}
