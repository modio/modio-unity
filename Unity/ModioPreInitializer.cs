using Modio.Extensions;
using UnityEngine;

namespace Modio.Unity
{
    public class ModioPreInitializer : MonoBehaviour
    {
        void Start()
        {
            if (!ModioClient.IsInitialized) ModioClient.Init().ForgetTaskSafely();
        }
    }
}
