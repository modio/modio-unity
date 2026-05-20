using System;
using System.Linq;
using Modio.Unity.UI.Components.ModProperties;
using UnityEngine;

namespace Modio.Unity.UI.Components
{
    /// <summary>
    /// These components subscribe to a parent <typeparamref name="TProperty"/> component and provide a 
    /// single point to hook up all supported customisation.
    /// These are provided via a drop down list when you click the `+` button.
    /// </summary>
    /// <typeparam name="TOwner">The parent type to hook up to e.g. <see cref="ModioUIMod"/></typeparam>
    /// <typeparam name="TProperty">The property that will be invoked e.g. <see cref="IModProperty"/></typeparam>
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

            if (_monoBehaviourEvents != null)
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
            if (_monoBehaviourEvents != null)
                foreach (IPropertyMonoBehaviourEvents monoBehaviourEvents in _monoBehaviourEvents)
                    monoBehaviourEvents.OnDisable();
        }

        protected abstract void UpdateProperties();
    }
}
