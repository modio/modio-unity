using Modio.Mods;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Modio.Unity.UI.Components
{
    public class ModioUIMod : MonoBehaviour, IModioUIPropertiesOwner, IPointerClickHandler, ISubmitHandler
    {
        public UnityEvent onModUpdate;
        public UnityEvent<Mod> onClickOrSubmit;
        public UnityEvent<ModioUIMod> onDisplayMoreInfo;

        public Mod Mod { get; private set; }

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

        void OnModUpdated() => onModUpdate?.Invoke();

        public void OnPointerClick(PointerEventData eventData) => OnSubmit(eventData);

        public void OnSubmit(BaseEventData eventData) => onClickOrSubmit?.Invoke(Mod);

        public void OnDisplayMoreInfoClicked() => onDisplayMoreInfo?.Invoke(this);
    }
}
