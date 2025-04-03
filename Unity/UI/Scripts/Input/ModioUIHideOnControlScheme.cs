using UnityEngine;

namespace Modio.Unity.UI.Input
{
    public class ModioUIHideOnControlScheme : MonoBehaviour
    {
        [SerializeField] bool _showOnController;
        [SerializeField] bool _showOnKBM;

        [SerializeField] GameObject[] _objectsToHide;

        void OnEnable()
        {
            ModioUIInput.SwappedControlScheme += OnSwappedToController;
            OnSwappedToController(ModioUIInput.IsUsingGamepad);
        }

        void OnDisable()
        {
            ModioUIInput.SwappedControlScheme -= OnSwappedToController;
        }

        void OnSwappedToController(bool isController)
        {
            foreach (GameObject gameObj in _objectsToHide)
            {
                gameObj.SetActive(isController ? _showOnController : _showOnKBM);
            }
        }
    }
}
