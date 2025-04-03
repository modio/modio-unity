using System;
using Modio.Mods;
using Modio.Unity.UI.Search;
using TMPro;
using UnityEngine;

namespace Modio.Unity.UI.Components.SearchProperties
{
    [Serializable]
    public class SearchPropertyTotalSize : ISearchProperty
    {
        [SerializeField] TMP_Text _totalFileSize;
        [SerializeField, Tooltip(StringFormat.BYTES_FORMAT_TOOLTIP)]
        StringFormatBytes _sizeFormat = StringFormatBytes.Suffix;
        [SerializeField, ShowIf(nameof(IsCustomFormat))]
        string _customSizeFormat;

        [SerializeField] ModioUIMod _alsoIncludeSizeOf;
        [SerializeField] bool _ignoreInstalledMods;

        bool IsCustomFormat() => _sizeFormat == StringFormatBytes.Custom;

        public void OnSearchUpdate(ModioUISearch search)
        {
            if (_totalFileSize != null)
            {
                var totalFileSize = 0L;

                if (_alsoIncludeSizeOf != null &&
                    _alsoIncludeSizeOf.Mod != null &&
                    (!_ignoreInstalledMods || _alsoIncludeSizeOf.Mod.File.State != ModFileState.Installed))
                {
                    totalFileSize += _alsoIncludeSizeOf.Mod.File.FileSize;
                }

                foreach (Mod mod in search.LastSearchResultMods)
                {
                    if (!_ignoreInstalledMods || mod.File.State != ModFileState.Installed)
                        totalFileSize += mod.File.FileSize;
                }

                _totalFileSize.text = StringFormat.Bytes(_sizeFormat, totalFileSize, _customSizeFormat);
            }
        }
    }
}
