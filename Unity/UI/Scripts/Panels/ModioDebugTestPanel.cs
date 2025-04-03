using Modio.Unity.UI.Input;

namespace Modio.Unity.UI.Panels
{
    /// <example>
    /// [ModioDebugMenu]
    /// static void ExampleButton()
    /// {
    ///     Debug.Log("Hello World");
    /// }
    /// </example>
    public class ModioDebugTestPanel : ModioPanelBase
    {
        bool _hasDoneHookup;
        ModioDebugMenu _modioDebugMenu;

        protected override void Awake()
        {
            _modioDebugMenu = GetComponent<ModioDebugMenu>();
            _modioDebugMenu.Awake();
        }

        void OnEnable()
        {
            if(ModioClient.Settings.TryGetPlatformSettings<ModioEnableDebugMenu>(out _))
                ModioUIInput.AddHandler(ModioUIInput.ModioAction.DeveloperMenu, OpenPanel);
        }

        void OnDisable()
        {
            ModioUIInput.RemoveHandler(ModioUIInput.ModioAction.DeveloperMenu, OpenPanel);
        }

        public override void OnGainedFocus(GainedFocusCause selectionBehaviour)
        {
            FindAllHookups();
            
            base.OnGainedFocus(selectionBehaviour);
            
            if(selectionBehaviour == GainedFocusCause.OpeningFromClosed) _modioDebugMenu.SetToDefaults();
        }

        void FindAllHookups()
        {
            if (_hasDoneHookup) return;
            _hasDoneHookup = true;
            
            _modioDebugMenu.AddAllMethodsOrPropertiesWithAttribute<ModioDebugMenuAttribute>(attribute => attribute.ShowInBrowserMenu);
        }
    }
}
