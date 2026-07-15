using System;
using System.Threading.Tasks;
using Modio.API;
using Modio.API.HttpClient;
using Modio.API.Interfaces;
using Modio.Authentication;
using Modio.Errors;
using Modio.Extensions;
using Modio.FileIO;
using Modio.Mods;
using Modio.Users;

namespace Modio
{
    public static class ModioClient
    {
        /// <summary>
        /// The Data Storage implementation being used by the plugin.
        /// </summary>
        /// <remarks>Prefer resolving the dependency yourself</remarks>
        /// <seealso cref="ModioServices"/>
        public static IModioDataStorage DataStorage => ModioServices.Resolve<IModioDataStorage>();

        /// <summary>
        /// The API interface being used by the plugin.
        /// </summary>
        /// <remarks>Prefer resolving the dependency yourself</remarks>
        /// <seealso cref="ModioServices"/>
        public static IModioAPIInterface Api => ModioServices.Resolve<IModioAPIInterface>();

        /// <summary>
        /// The Authentication Service being used by the plugin.
        /// </summary>
        /// <remarks>Prefer resolving the dependency yourself</remarks>
        /// <seealso cref="ModioServices"/>
        public static IModioAuthService AuthService => ModioServices.Resolve<IModioAuthService>();

        /// <summary>
        /// Returns the <see cref="ModioSettings"/> from the ModioServices
        /// </summary>
        /// <remarks>Prefer resolving the dependency yourself</remarks>
        /// <seealso cref="ModioServices"/>
        public static ModioSettings Settings => ModioServices.Resolve<ModioSettings>();

        
        /// <summary>
        /// Returns <c>true</c> if initialized, <c>false</c> otherwise.
        /// </summary>
        public static bool IsInitialized { get; private set; } = false;
        /// <summary> If we are in the process of initializing, but it's not complete yet </summary>
        internal static bool IsCurrentlyInitializing => _initializingTcs != null;
        
        static TaskCompletionSource<Error> _initializingTcs;
        static TaskCompletionSource<bool> _shutdownTcs;

        static bool _hasBoundDefaultServices;

        static event Action InternalOnInitialized;

        /// <summary>
        /// Event that is invoked when the client is initialized.
        /// If the client is already initialized when a listener is added
        /// the listener is immediately invoked.
        /// </summary>
        public static event Action OnInitialized
        {
            add
            {
                InternalOnInitialized +=
                    value;

                if (IsInitialized) value?.Invoke();
            }

            remove => InternalOnInitialized -= value;
        }

        /// <summary>
        /// Even that is invoked when the client is shutdown.
        /// </summary>
        public static event Action OnShutdown;

        /// <summary>
        /// Initializes the ModioClient with the given <see cref="ModioSettings"/>
        /// </summary>
        /// <param name="settings" >The settings to use</param>
        /// <returns>
        /// An asynchronous task that returns <see cref="Error"/>.<see cref="Error.None"/> on success.
        /// </returns>
        public static Task<Error> Init(ModioSettings settings)
        {
            ModioServices.BindInstance(settings, ModioServicePriority.PlatformProvided);

            return Init();
        }
        
        /// <summary>
        /// Initializes the ModioClient.
        /// </summary>
        /// <returns>
        /// A task that returns <see cref="Error"/>.
        /// If successfully initialized returns <see cref="Error"/>.<see cref="Error.None"/>
        /// </returns>
        public static async Task<Error> Init()
        {
            if (IsInitialized)
            {
                ModioLog.Error?.Log($"Reinitializing mod.io SDK! Use {nameof(ModioClient)}.{nameof(Shutdown)} before initializing the SDK!");
                return new Error(ErrorCode.SDKALREADY_INITIALIZED);
            }
            
            if(_initializingTcs != null && _shutdownTcs != null)
            {
                ModioLog.Error?.Log("You have attempted to initializing the mod.io SDK, while it is already initializing and shutting down. "
                                    + "Waiting for the Init and Shutdown to complete, which may cause undesirable delays");

                await Task.WhenAll(_shutdownTcs.Task, _initializingTcs.Task);
            }
            if(_shutdownTcs != null)
            {
                ModioLog.Warning?.Log("You have started initializing the mod.io SDK while it is shutting down. Waiting for the shutdown to complete first, which may cause undesirable delays");
                await _shutdownTcs.Task;
            }
            
            BindDefaultServices();

            if(!ModioServices.TryResolve(out ModioSettings settings) || settings == null)
            {
                ModioLog.Error?.Log("mod.io SDK failed to find required settings");
                return new Error(ErrorCode.MISSING_COMPONENTS);
            }

            if (string.IsNullOrEmpty(settings.APIKey))
            {
                ModioLog.Error?.Log("mod.io SDK failed to find valid API key in settings");
                return new Error(ErrorCode.INVALID_APIKEY);
            }
            
            if(settings.GameId <= 0)
            {
                ModioLog.Error?.Log("mod.io SDK failed to find valid Game ID in settings");
                return new Error(ErrorCode.INVALID_GAME_ID);
            }
            
            if (DataStorage == null || Api == null)
            {
                ModioLog.Error?.Log("mod.io SDK failed to find required components");
                return new Error(ErrorCode.MISSING_COMPONENTS);
            }

            if (_initializingTcs != null) return await _initializingTcs.Task;

            _initializingTcs = new TaskCompletionSource<Error>();
            
            ModioAPI.Init();
            ModioAPI.SetResponseLanguage(Settings.DefaultLanguage);

            Error error = await DataStorage.Init();

            if (error)
            {
                ModioLog.Error?.Log("mod.io SDK failed to init DataStorage module");
                _initializingTcs.TrySetResult(error);
                _initializingTcs = null;
                return error;
            }
            
            GameData.GetGameData().ForgetTaskSafely();
            
            await User.InitializeNewUser();
            
            error = await ModInstallationManagement.Init();

            if (error)
            {
                ModioLog.Error?.Log($"mod.io SDK failed to Init {typeof(ModInstallationManagement)}");
                _initializingTcs.TrySetResult(error);
                _initializingTcs = null;
                return error;
            }
            
            IsInitialized = true;
            InternalOnInitialized?.Invoke();
            _initializingTcs.TrySetResult(Error.None);
            _initializingTcs = null;

            return Error.None;
        }
        
        /// <summary>
        /// Shuts down the client.
        /// Will invoke the shutdown methods on services.
        /// </summary>
        public static async Task Shutdown()
        {
            
            if(_initializingTcs != null && _shutdownTcs != null)
            {
                ModioLog.Warning?.Log("You have started initializing and shutting down the mod.io SDK at the same time. Waiting for the Init and Shutdown to complete, which may cause undesirable delays");
                
                return;
            }
            if (_initializingTcs != null)
            {
                ModioLog.Warning?.Log("You have shutdown the mod.io SDK while is is initializing. Waiting for the Init to complete first, which may cause undesirable delays");
                
                await _initializingTcs.Task;
            }

            if (!IsInitialized)
            {
                ModioLog.Warning?.Log("Attempted to shutdown mod.io SDK when is not initialized. Ignoring.");
                return;
            }

            IsInitialized = false;

            if (_shutdownTcs != null)
            {
                await _shutdownTcs.Task;
                return;
            }

            _shutdownTcs = new TaskCompletionSource<bool>();
            
            await User.Shutdown();
            
            ModioServices.RemoveBindingChangedListener<ModioSettings>(ErrorOnBindSettings);
                
            OnShutdown?.Invoke();

            await ModInstallationManagement.Shutdown();
            
            if (ModioServices.TryResolve(out IModioDataStorage dataStorage))
                await dataStorage.Shutdown();
            
            UnbindDefaultServices();
            _shutdownTcs.TrySetResult(true);
            _shutdownTcs = null;

        }
        
        static void BindDefaultServices()
        { 
            if(_hasBoundDefaultServices) return;
            _hasBoundDefaultServices = true;
            
            ModioServices.AddBindingChangedListener<ModioSettings>(ErrorOnBindSettings);

            ModioServices.Bind<IModioAPIInterface>()
                         .FromNew<ModioAPIHttpClient>(ModioServicePriority.Default);
            
            ModioServices.Bind<IModioRootPathProvider>()
                         .FromNew<DefaultRootPathProvider>(ModioServicePriority.Default);
            
            ModioServices.Bind<IModioDataStorage>()
                         .FromNew<BaseDataStorage>(ModioServicePriority.Default);
            
            ModioServices.Bind<ModioEmailAuthService>()
                         .WithInterfaces<IGetActiveUserIdentifier>()
                         .WithInterfaces<IModioAuthService>()
                         .FromNew<ModioEmailAuthService>(ModioServicePriority.Default);
            
            
            ModioServices.BindErrorMessage<ModioSettings>(
                "Please ensure you've bound a ModioSettings using " +
                "ModioServices.BindInstance(settings); before trying to use Modio classes");
        }

        static void UnbindDefaultServices()
        {
            if(!_hasBoundDefaultServices) return;
            _hasBoundDefaultServices = false;
            
            ModioServices.RemoveBindingChangedListener<ModioSettings>(ErrorOnBindSettings);
            
            
            ModioServices.RemoveBindingWithPriority<IModioAPIInterface>(ModioServicePriority.Default);
            
            ModioServices.RemoveBindingWithPriority<IModioRootPathProvider>(ModioServicePriority.Default);
            
            ModioServices.RemoveBindingWithPriority<IModioDataStorage>(ModioServicePriority.Default);
            
            ModioServices.RemoveBindingWithPriority<ModioEmailAuthService>(ModioServicePriority.Default);
            
            ModioServices.RemoveBindingWithPriority<IGetActiveUserIdentifier>(ModioServicePriority.Default);
            
            ModioServices.RemoveBindingWithPriority<IModioAuthService>(ModioServicePriority.Default);

            
        }
        
        static void ErrorOnBindSettings(ModioSettings _)
        {
            if (IsInitialized || IsCurrentlyInitializing)
                ModioLog.Error?.Log("You have changed the ModioSettings after the ModioClient has been initialized. This may cause unexpected behaviour, please ensure you set all required settings before initialization");
        }
        
    }
}
