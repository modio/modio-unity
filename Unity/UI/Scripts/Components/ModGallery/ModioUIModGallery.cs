using System.Collections.Generic;
using System.Linq;
using Modio.Images;
using Modio.Mods;
using Modio.Unity.UI.Input;
using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Components.ModGallery
{
    public class ModioUIModGallery : ModioUIModProperties
    {
        [SerializeField] RawImage _image;
        [SerializeField] Mod.GalleryResolution _resolution = Mod.GalleryResolution.X1280_Y720;
        [SerializeField] bool _useHighestAvailableResolutionAsFallback = true;
        [SerializeField] ModioUIModGalleryPagination _paginationTemplate;
        [SerializeField] int _max = 10;
        [SerializeField] bool _wrap = true;
        [Space]
        [Tooltip("(Optional) Active while loading, inactive once loaded."), SerializeField]
        GameObject _loadingActive;
        [Tooltip("(Optional) Inactive while loading, active once loaded."), SerializeField]
        GameObject _loadedActive;

        Mod _mod;
        int _galleryCount;
        int _index;

        readonly List<ModioUIModGalleryPagination> _pagination = new List<ModioUIModGalleryPagination>();
        LazyImage<Texture2D> _lazyImage;

        protected override void Awake()
        {
            base.Awake();

            if (_paginationTemplate != null)
            {
                _paginationTemplate.Gallery = this;
                _pagination.Add(_paginationTemplate);
            }
        }

        void OnDisable()
        {
            ModioUIInput.RemoveHandler(ModioUIInput.ModioAction.TabLeft,  Prev);
            ModioUIInput.RemoveHandler(ModioUIInput.ModioAction.TabRight, Next);
        }

        protected override void UpdateProperties()
        {
            if (Owner.Mod == null) return;

            if (Owner.Mod != _mod) SetMod(Owner.Mod);

            UpdateTabListener();

            GoTo(_index);
        }

        void SetMod(Mod mod)
        {
            _mod = mod;
            _galleryCount = Mathf.Min(mod.Gallery.Length, _max);
            _index = 0;

            if (_pagination.Any())
            {
                for (int i = _pagination.Count; i < _galleryCount; i++)
                {
                    ModioUIModGalleryPagination pagination = Instantiate(
                        _pagination[0],
                        _pagination[0].transform.parent
                    );

                    pagination.Gallery = this;
                    pagination.Index = i;

                    _pagination.Add(pagination);
                }

                for (int i = 0; i < _pagination.Count; i++)
                    _pagination[i].gameObject.SetActive(_galleryCount > 1 && i < _galleryCount);
            }
        }

        void UpdateTabListener()
        {
            ModioUIInput.RemoveHandler(ModioUIInput.ModioAction.TabLeft,  Prev);
            ModioUIInput.RemoveHandler(ModioUIInput.ModioAction.TabRight, Next);

            if (_galleryCount > 1)
            {
                ModioUIInput.AddHandler(ModioUIInput.ModioAction.TabLeft,  Prev);
                ModioUIInput.AddHandler(ModioUIInput.ModioAction.TabRight, Next);
            }
        }

        public void GoTo(int index)
        {
            index = _galleryCount != 0 ? (index + _galleryCount) % _galleryCount : 0;
            
            _lazyImage ??= new LazyImage<Texture2D>(
                ImageCacheTexture2D.Instance,
                texture2D =>
                {
                    if (_image != null) _image.texture = texture2D;
                },
                isLoading =>
                {
                    if (_loadingActive) _loadingActive.SetActive(isLoading);
                    if (_loadedActive) _loadedActive.SetActive(!isLoading);
                }
            );
            
            if (_galleryCount > 0) 
                _lazyImage.SetImage(_mod.Gallery[index], _resolution);
            else
                _lazyImage.SetImage(_mod.Logo, (Mod.LogoResolution)_resolution);

            if (_pagination.Any())
                for (var i = 0; i < _galleryCount; i++)
                    _pagination[i].SetState(i == index);
            
            _index = index;
        }

        public void Prev()
        {
            if (_wrap || _index > 0) GoTo(_index - 1);
        }

        public void Next()
        {
            if (_wrap || _index < _galleryCount - 1) GoTo(_index + 1);
        }
    }
}
