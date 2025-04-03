using UnityEngine;

namespace Modio.Unity.UI.Input
{
    public class ModioUIActionSender : MonoBehaviour
    {
        [SerializeField] ModioUIInput.ModioAction _action;

        public void PressedAction()
        {
            ModioUIInput.PressedAction(_action);
        }
    }
}
