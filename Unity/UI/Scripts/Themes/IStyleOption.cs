namespace Modio.Unity.UI.Scripts.Themes
{
    public interface IStyleOption
    {
        ThemeOptions OptionType { get; }
        void TryStyleComponent(UnityEngine.Object component);
    }

    public interface IStyleComponent<in T> : IStyleOption where T : UnityEngine.Object
    {
        void StyleComponent(T component);
    }
}
