using System.Collections;
using ModIO.Util;
using ModIOBrowser.Implementation;
using UnityEngine;
using UnityEngine.UI;

namespace ModIOBrowser
{
    internal class ColorApplier<T> : MonoBehaviour where T : Graphic
    {
        public ColorSetterType color;
        protected virtual T graphic => null;
        public ColorScheme colorScheme;

        private void Start()
        {
            if(!Apply())
            {
                CoroutineRunner.Instance.Run(AttemptToRecolorSoon());
            }
        }

        private bool Apply()
        {
            var scheme = colorScheme == null ? Browser.Instance.colorScheme : colorScheme;
            if(scheme != null)
            {
                graphic.color = scheme.GetSchemeColor(color);
                return true;
            }
            else
            {
                Debug.LogError($"Unable to apply color to { transform.FullPath() }");
            }

            return false;
        }

        IEnumerator AttemptToRecolorSoon()
        {
            yield return new WaitForEndOfFrame();
            while(true)
            {
                if(Apply())
                {
                    yield break;
                }

                Debug.LogError($"Unable to set color scheme to {transform.FullPath()} - is this intentional? Attempting again...");
                yield return new WaitForFixedUpdate();
            }
        }
    }

    //Other types are easy to extend to, just inherit from ColorApplier and apply type
    //If this is a regular occurance for you, you may want to consider extending Graphic as an interface
    //to allow for non-unity Graphic types
    
    internal class ColorApplierImage : ColorApplier<Image>
    {
        public Image image;
        override protected Image graphic => image;

        private void OnValidate()
        {
            if(image == null)
            {
                image =  GetComponent<Image>();
            }
        }
    }
}
