namespace Modio.Unity.UI.Scripts.Themes
{
    public interface IStyleOption
    {
        ThemeOptions OptionType { get; }
        void TryStyleComponent(UnityEngine.Object component);
    }
}
