using UnityEngine.Events;

namespace Modio.Unity.UI.Components
{
    public interface IModioUIPropertiesOwner
    {
        void AddUpdatePropertiesListener(UnityAction listener);

        void RemoveUpdatePropertiesListener(UnityAction listener);
    }
}
