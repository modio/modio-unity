using Modio.Mods;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Modio.Unity.UI.Components
{
    /// <summary>
    /// A UI container for a mod. Assign a mod to this, and any
    /// child <see cref="ModioUIModProperties"/> will get updates about the mod
    /// </summary>
    /// <remarks>This will subscribe to the <see cref="Modio.Mods.Mod"/>'s `OnModUpdated` event and post updates to all child properties automatically.</remarks>
    public class ModioUIMod : MonoBehaviour, IModioUIPropertiesOwner, IModioUIResourceContainer<Mod>, IPointerClickHandler, ISubmitHandler
    {
        public UnityEvent onModUpdate;
        public UnityEvent<Mod> onClickOrSubmit;
        public UnityEvent<ModioUIMod> onDisplayMoreInfo;

        public Mod Mod { get; private set; }
        Mod IModioUIResourceContainer<Mod>.Resource => Mod;
        bool IModioUIResourceContainer<Mod>.IsPlaceholderUI { get; set; }

        void OnDestroy()
        {
            if (Mod != null) Mod.OnModUpdated -= OnModUpdated;
        }

        public void AddUpdatePropertiesListener(UnityAction listener) => onModUpdate.AddListener(listener);

        public void RemoveUpdatePropertiesListener(UnityAction listener) => onModUpdate.RemoveListener(listener);

        public void SetMod(Mod mod)
        {
            if (Mod != null) Mod.OnModUpdated -= OnModUpdated;

            Mod = mod;

            if (mod == null) return;

            Mod.OnModUpdated += OnModUpdated;
            OnModUpdated();
        }
        void IModioUIResourceContainer<Mod>.SetResource(Mod resource) => SetMod(resource);

        void OnModUpdated() => onModUpdate?.Invoke();

        public void OnPointerClick(PointerEventData eventData) => OnSubmit(eventData);

        public void OnSubmit(BaseEventData eventData) => onClickOrSubmit?.Invoke(Mod);

        public void OnDisplayMoreInfoClicked() => onDisplayMoreInfo?.Invoke(this);
    }
}
