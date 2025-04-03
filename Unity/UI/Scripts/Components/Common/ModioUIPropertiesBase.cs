using System;
using System.Linq;
using UnityEngine;

namespace Modio.Unity.UI.Components
{
    public abstract class ModioUIPropertiesBase<TOwner, TProperty> : MonoBehaviour
        where TOwner : Component, IModioUIPropertiesOwner
    {
        protected abstract TProperty[] Properties { get; }

        protected TOwner Owner;

        IPropertyMonoBehaviourEvents[] _monoBehaviourEvents;

        protected virtual void Awake()
        {
            Owner = GetComponentInParent<TOwner>();

            if (Owner != null)
            {
                Owner.AddUpdatePropertiesListener(UpdateProperties);

                _monoBehaviourEvents = Properties.Any(property => property is IPropertyMonoBehaviourEvents)
                    ? Properties.OfType<IPropertyMonoBehaviourEvents>().ToArray()
                    : Array.Empty<IPropertyMonoBehaviourEvents>();

                return;
            }

            Debug.LogWarning($"{GetType().Name} {gameObject.name} could not find a {nameof(TOwner)}, disabling.", this);
            enabled = false;
        }

        protected virtual void Start()
        {
            foreach (IPropertyMonoBehaviourEvents monoBehaviourEvents in _monoBehaviourEvents)
                monoBehaviourEvents.Start();

            UpdateProperties();
        }

        protected void OnDestroy()
        {
            if (Owner) Owner.RemoveUpdatePropertiesListener(UpdateProperties);

            foreach (IPropertyMonoBehaviourEvents monoBehaviourEvents in _monoBehaviourEvents)
                monoBehaviourEvents.OnDestroy();
        }

        void OnEnable()
        {
            foreach (IPropertyMonoBehaviourEvents monoBehaviourEvents in _monoBehaviourEvents)
                monoBehaviourEvents.OnEnable();
        }

        void OnDisable()
        {
            foreach (IPropertyMonoBehaviourEvents monoBehaviourEvents in _monoBehaviourEvents)
                monoBehaviourEvents.OnDisable();
        }

        protected abstract void UpdateProperties();
    }
}
