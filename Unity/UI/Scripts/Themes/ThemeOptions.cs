namespace Modio.Unity.UI.Scripts.Themes
{
    public enum ThemeOptions
    {
        BackgroundColor,
        ForegroundColor,
        ButtonBackgroundColor,
        HighlightColor,
        OutlineColor,
        DisabledColor,
        TextColor,
        SecondaryTextColor,
        HighlightedTextColor,
        BackgroundImage,
        FontStandard,
        FontMedium,
        FontBold,
    }

    public enum StyleTarget
    {
        None,
        Default,
        Button = 100,
        ButtonPrimaryAction,
        ButtonSpecial,
        ButtonTag,
        ButtonRadio,
        ActionBindings,
        ButtonPrivacyPolicy,
        ActionBindingGlyph,
        ButtonBrowserTab,
        ButtonWalletBalance,
        ButtonHamburger,
        ButtonModCreator,
        PanelBackground = 200,
        PanelModDisplay,
        PanelModTileOptions,
        PanelModDisplayInfoBackground,
        PanelFilter,
        FilterToggle = 300,
        TextSubdued,
        InputField,
        ModStat,
        ModDisplayLibraryStatus,
        ModTile = 400,
        BrowserBackground,
    }
}
