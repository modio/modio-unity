using UnityEngine;
using TMPro;
using static ModIO.Utility;
#if UNITY_EDITOR
#endif

namespace ModIOBrowser.Implementation
{
    public class UiTranslatorTest : MonoBehaviour
    {
        public TMP_Text testText;
#pragma warning disable 0649 //it is allocated
        private Translation reference;
#pragma warning restore 0649


        private void Awake()
        {
            Translation.Get(reference, "Subscribed", s =>
            {
                Debug.Log("setting " + s);
                testText.text = s;
                testText.ForceMeshUpdate(true);
            });
        }

        [ExposeMethodInEditor]
        public void PokeTranslator()
        {
            SimpleMessageHub.Instance.Publish(new MessageUpdateTranslations());
        }
    }
}
