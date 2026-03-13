using System;
using Modio.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Modio.Unity.UI.Components
{
    public class ModioUICollection : MonoBehaviour, IModioUIPropertiesOwner, IModioUIResourceContainer<ModCollection>, IPointerClickHandler, ISubmitHandler
    {
        public UnityEvent onCollectionUpdate;
        public UnityEvent<ModCollection> onClickOrSubmit;
        public UnityEvent<ModioUICollection> onDisplayMoreInfo;

        public ModCollection Collection { get; private set; }
        ModCollection IModioUIResourceContainer<ModCollection>.Resource => Collection;
        bool IModioUIResourceContainer<ModCollection>.IsPlaceholderUI { get; set; }

        void OnDestroy()
        {
            if (Collection != null) Collection.OnModCollectionUpdated -= CollectionUpdated;
        }

        public void AddUpdatePropertiesListener(UnityAction listener) => onCollectionUpdate.AddListener(listener);

        public void RemoveUpdatePropertiesListener(UnityAction listener) => onCollectionUpdate.RemoveListener(listener);

        public void SetCollection(ModCollection collection)
        {
            if (Collection != null) Collection.OnModCollectionUpdated-= CollectionUpdated;

            Collection = collection;

            if (collection == null) return;

            Collection.OnModCollectionUpdated += CollectionUpdated;
            CollectionUpdated();
        }
        void IModioUIResourceContainer<ModCollection>.SetResource(ModCollection resource) => SetCollection(resource);

        void CollectionUpdated() => onCollectionUpdate?.Invoke();

        public void OnPointerClick(PointerEventData eventData) => OnSubmit(eventData);

        public void OnSubmit(BaseEventData eventData) => onClickOrSubmit?.Invoke(Collection);

        public void OnDisplayMoreInfoClicked() => onDisplayMoreInfo?.Invoke(this);
    }
}
