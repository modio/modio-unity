using System;
using System.IO;
using System.Threading.Tasks;
using ModIO.Implementation.API;
using UnityEngine;

namespace ModIO.Implementation
{
    public class ModioPlatform
    {
        static ModioPlatform _activePlatform;
        public static ModioPlatform ActivePlatform
        {
            get
            {
                if(_activePlatform != null)
                    return _activePlatform;

                if (IsInitializing)
                    Logger.Log(LogLevel.Warning, $"Platform Initialization still in progress! Returning null");
                else if (!Application.isConsolePlatform )
                    _activePlatform = new ModioPlatform();
                else
                    Logger.Log(LogLevel.Error, "You must set a ModioPlatform before calling some Modio classes on consoles");

                return _activePlatform;
            }
            protected set
            {
                if (_activePlatform != null)
                {
                    Logger.Log(LogLevel.Warning, $"Overriding active ModioPlatform to {value} after it was already set to {_activePlatform}."
                                                 + "Any previously called methods may have been called on the previous one");
                }
                _activePlatform = value;
                isInitializing = false;
            }
        }

        /// <summary>Is the platform ready to be accessed?</summary>
        public static bool IsInitializing => isInitializing;

        static bool isInitializing = false;


        /// <summary>
        /// Set to true if you need smaller applet friendly pages on your platform
        /// </summary>
        public virtual bool WebBrowserNeedsSimplePages => false;

        public virtual void OpenWebPage(string url)
        {
            Application.OpenURL(url);
        }

        public virtual bool TryOpenVirtualKeyboard(string title,
                                                   string text,
                                                   string placeholder,
                                                   ModioVirtualKeyboardType virtualKeyboardType,
                                                   int characterLimit,
                                                   bool multiline,
                                                   Action<string> onClose)
        {
            return false;
        }

        public virtual bool TokenPackAvailableOnPlatform(TokenPack tokenPack)
        {
            if (GetPortalForPlatform(tokenPack, out _)) return true;

            return false;
        }

        protected static bool GetPortalForPlatform(TokenPack tokenPack, out TokenPack.Portal portal)
        {
            var portalShortCode = ServerConstants.ConvertUserPortalToHeaderValue(Settings.build.userPortal);
            foreach (var tokenPackPortal in tokenPack.portals)
            {
                if (tokenPackPortal.portal == portalShortCode)
                {
                    portal = tokenPackPortal;
                    return true;
                }
            }
            portal = default;
            return false;
        }

        public virtual Task<Result> OpenPlatformPurchaseFlow()
        {
            Debug.LogError($"not yet implemented: opening platform store");
            return Task.FromResult(ResultBuilder.Unknown);
        }

        public virtual bool TryGetAvailableDiskSpace(out long availableFreeSpace)
        {
            availableFreeSpace = 0;
#if !ENABLE_IL2CPP
            string persistentRootDirectory = DataStorage.persistent?.RootDirectory;
            if (persistentRootDirectory == null)
            {
                return false;
            }
            FileInfo f = new FileInfo(persistentRootDirectory);
            string drive = Path.GetPathRoot(f.FullName);
            DriveInfo d = new DriveInfo(drive);
            availableFreeSpace = d.AvailableFreeSpace;
            return true;
#endif
            return false;
        }

        /// <summary>Tells the plugin that while the platform is not ready to be accessed,
        /// it is being initialized and should wait until the platform is set.</summary>
        public static void SetInitializing() => isInitializing = true;
    }
}
