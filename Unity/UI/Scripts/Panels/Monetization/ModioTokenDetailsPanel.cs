using System;
using System.Threading.Tasks;
using Modio.Errors;
using Modio.Extensions;
using Modio.Images;
using Modio.Mods;
using Modio.Monetization;
using Modio.Unity.UI.Components;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Modio.Unity.UI.Panels.Monetization
{
    public class ModioTokenDetailsPanel : ModioPanelBase
    {
        [SerializeField] TMP_Text _tokenTitle;
        [SerializeField] TMP_Text _tokenDescription;
        [SerializeField] TMP_Text _tokenPrice;
        [SerializeField] RawImage _tokenImage;
        [SerializeField] AspectRatioFitter _tokenImageAspectRatioFitter;
        [SerializeField] GameObject _imageLoadingContainer;
        LazyImage<Texture2D> _lazyImage;

        PortalSku _tokenPack;

        public void OpenPanel(PortalSku tokenPack)
        {
            _tokenPack = tokenPack;
            if (_tokenTitle) _tokenTitle.text = tokenPack.Name;
            if (_tokenDescription) _tokenDescription.text = tokenPack.Description;
            if (_tokenPrice) _tokenPrice.text = tokenPack.FormattedPrice;

            UpdateTokenImage(tokenPack);

            OpenPanel();
        }

        void UpdateTokenImage(PortalSku tokenPack)
        {
            if (!_tokenImage) return;
            if (!_imageLoadingContainer) return;
            if (tokenPack.ImageReference == default(ImageReference)) return;

            _lazyImage ??= new LazyImage<Texture2D>(
                ImageCacheTexture2D.Instance,
                texture2D =>
                {
                    if (_tokenImageAspectRatioFitter)
                        _tokenImageAspectRatioFitter.aspectRatio = (float)texture2D.width / texture2D.height;

                    _tokenImage.texture = texture2D;
                },
                b =>
                {
                    _imageLoadingContainer.gameObject.SetActive(b);
                    _tokenImage.gameObject.SetActive(!b);
                }
            );

            _lazyImage.SetImage(tokenPack.ImageReference);
        }

        public void ConfirmPurchase()
        {
            if (!ModioServices.TryResolve(out IModioVirtualCurrencyProviderService skuProvider))
            {
                ModioPanelManager.GetPanelOfType<ModioTokenDetailsPanel>().ClosePanel();
                return;
            }

            Task<Error> platformPurchaseFlowTask = skuProvider.OpenCheckoutFlow(_tokenPack);

            if (platformPurchaseFlowTask != null)
                ModioPanelManager.GetPanelOfType<ModioWaitingPanelGeneric>()
                                 .OpenAndWaitFor(
                                     platformPurchaseFlowTask,
                                     error =>
                                     {
                                         if (error)
                                         {
                                             if (error.Code == ErrorCode.OPERATION_CANCELLED) return;

                                             ModioPanelManager.GetPanelOfType<ModioErrorPanelGeneric>()
                                                              .OpenPanel(error);
                                         }

                                         ClosePanel();
                                         var buyPanel = ModioPanelManager.GetPanelOfType<ModioBuyTokensPanel>();
                                         if (buyPanel != null) buyPanel.ClosePanel();
                                     }
                                 );
        }
    }
}
