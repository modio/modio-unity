using System;
using System.Threading.Tasks;
using Modio.API;
using Modio.API.HttpClient;
using Modio.API.Interfaces;
using Modio.Authentication;
using Modio.Errors;
using Modio.FileIO;
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
        internal static bool IsCurrentlyInitializing => _initializingTCS != null;
        
        static TaskCompletionSource<Error> _initializingTCS;
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
            
            BindDefaultServices();

            if (DataStorage == null || Api == null)
            {
                ModioLog.Error?.Log("mod.io SDK failed to find required components");
                return new Error(ErrorCode.MISSING_COMPONENTS);
            }

            if (_initializingTCS != null) return await _initializingTCS.Task;

            _initializingTCS = new TaskCompletionSource<Error>();
            
            ModioAPI.Init();
            ModioAPI.SetResponseLanguage(Settings.DefaultLanguage);

            Error error = await DataStorage.Init();

            if (error)
            {
                ModioLog.Error?.Log("mod.io SDK failed to init DataStorage module");
                _initializingTCS.TrySetResult(error);
                _initializingTCS = null;
                return error;
            }
            
            await User.InitializeNewUser();
            
            error = await ModInstallationManagement.Init();

            if (error)
            {
                ModioLog.Error?.Log($"mod.io SDK failed to Init {typeof(ModInstallationManagement)}");
                _initializingTCS.TrySetResult(error);
                _initializingTCS = null;
                return error;
            }
            
            IsInitialized = true;
            InternalOnInitialized?.Invoke();
            _initializingTCS.TrySetResult(Error.None);
            _initializingTCS = null;

            return Error.None;
        }
        
        /// <summary>
        /// Shuts down the client.
        /// Will invoke the shutdown methods on services.
        /// </summary>
        public static async Task Shutdown()
        {
            IsInitialized = false;
            
            OnShutdown?.Invoke();

            await ModInstallationManagement.Shutdown();
            
            if (ModioServices.TryResolve(out IModioDataStorage dataStorage))
                await dataStorage.Shutdown();
        }
        
        static void BindDefaultServices()
        { 
            if(_hasBoundDefaultServices) return;
            _hasBoundDefaultServices = true;
            
            ModioServices.Bind<IModioAPIInterface>()
                         .FromNew<ModioAPIHttpClient>(ModioServicePriority.Default);
            
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
    }
}
