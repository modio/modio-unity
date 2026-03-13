using Modio.Mods;
using UnityEngine.Events;

namespace Modio.Unity.UI.Components
{
    public interface IModioUIPropertiesOwner
    {
        void AddUpdatePropertiesListener(UnityAction listener);

        void RemoveUpdatePropertiesListener(UnityAction listener);
    }

    public interface IModioUIResourceContainer<TResource> where TResource : IModioResource
    {
        TResource Resource { get; }
        
        void SetResource(TResource resource);
        
        bool IsPlaceholderUI { get; set; }
    }
}
