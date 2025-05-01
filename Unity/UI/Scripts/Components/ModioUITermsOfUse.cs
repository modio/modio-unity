using System.Threading.Tasks;
using Modio.Extensions;
using Modio.Platforms;
using TMPro;
using UnityEngine;

namespace Modio.Unity.UI.Components
{
    public class ModioUITermsOfUse : MonoBehaviour
    {
        [SerializeField] TMP_Text _termsOfUseText;
        [SerializeField] TMP_Text _agreeText;
        [SerializeField] TMP_Text _disagreeText;

        [SerializeField] TMP_Text _termsOfUseLinkButtonText;
        [SerializeField] TMP_Text _privacyPolicyLinkButtonText;

        static TermsOfUse _termsOfUse = null;
        static IWebBrowserHandler _browserHandler = null;


        public void Start()
        {
            if (_termsOfUse != null)
                ApplyTermsOfUse();
            else
                ModioClient.OnInitialized += OnPluginReady;
        }

        void OnPluginReady()
        {
            //Fetch the terms ASAP
            GetTermsOfUse().ForgetTaskSafely();

            _browserHandler = ModioServices.Resolve<IWebBrowserHandler>();

        }

        async Task GetTermsOfUse()
        {
            (Error error, TermsOfUse result) = await TermsOfUse.Get();

            if (error)
            {
                if (!error.IsSilent) ModioLog.Error?.Log($"Error getting terms of use: {error.GetMessage()}");
                return;
            }

            _termsOfUse = result;
            ApplyTermsOfUse();
        }

        void ApplyTermsOfUse()
        {
            if (_termsOfUse == null)
            {
                ModioLog.Error?.Log($"Attempted to apply terms of use before they were loaded");
                return;
            }

            if (_termsOfUseText != null) _termsOfUseText.text = _termsOfUse.TermsText;
            if (_agreeText != null) _agreeText.text = _termsOfUse.AgreeText;
            if (_disagreeText != null) _disagreeText.text = _termsOfUse.DisagreeText;

            if (_termsOfUseLinkButtonText != null) _termsOfUseLinkButtonText.text = GetLinkButtonText(LinkType.Terms);
            if (_privacyPolicyLinkButtonText != null) _privacyPolicyLinkButtonText.text = GetLinkButtonText(LinkType.Privacy);
        }

        public void HyperLinkToTOS()
        {
            HyperlinkTo(LinkType.Terms, "https://mod.io/terms");
        }

        public void HyperLinkToPrivacyPolicy()
        {
            HyperlinkTo(LinkType.Privacy, "https://mod.io/privacy");
        }

        public void HyperLinkToRefundPolicy()
        {
            HyperlinkTo(LinkType.Refund, "https://mod.io/refund");
        }

        string GetLinkButtonText(LinkType type)
        {
            if (_termsOfUse == null)
            {
                ModioLog.Error?.Log($"Attempted to get button text for {type} before terms of use loaded");
                return type.ToString();
            }

            foreach (TermsOfUseLink link in _termsOfUse.Links)
            {
                if (link.type == type)
                {
                    return link.text;
                }
            }

            ModioLog.Error?.Log($"Couldn't find TermsOfUseLink button text for {type}");

            return type.ToString();
        }

        static void HyperlinkTo(LinkType type, string fallbackLink)
        {
            string url = null;

            if (_termsOfUse == null)
                Debug.LogError($"Attempted to open hyperlink to {type} before terms of use loaded. Using fallback");
            else
                url = _termsOfUse.GetLink(type).url;

            if (string.IsNullOrWhiteSpace(url)) url = fallbackLink;

            _browserHandler?.OpenUrl(url);
        }

    }
}
