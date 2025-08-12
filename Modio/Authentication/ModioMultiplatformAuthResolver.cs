using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Modio.API;

namespace Modio.Authentication
{
    public class ModioMultiplatformAuthResolver : IModioAuthService, IGetActiveUserIdentifier, IPotentialModioEmailAuthService, IGetPortalProvider
    {
        const ModioServicePriority SERVICE_BINDING_PRIORITY = ModioServicePriority.PlatformProvided + 9;
        
        static bool _resolveUsingThis;
        static bool _hasInitialized;

        public static IModioAuthService ServiceOverride { get; set; }
        public static IReadOnlyList<IModioAuthService> AuthBindings { get; private set; }
        
        public static void Initialize()
        {
            if (_hasInitialized) return;
            _hasInitialized = true;
            AuthBindings = ModioServices.GetBindings<IModioAuthService>()
                                              .ResolveAll()
                                              .OrderByDescending(platformPair => platformPair.priority)
                                              .Select(platformPair => platformPair.service)
                                              .Where(platform => platform is IGetActiveUserIdentifier)
                                              .ToList();

            foreach (IModioAuthService service in AuthBindings)
            {
                ModioLog.Warning?.Log(service.GetType().Name);
            }
            
            ServiceOverride = AuthBindings.FirstOrDefault();
            
            ModioServices.Bind<ModioMultiplatformAuthResolver>()
                              .WithInterfaces<IModioAuthService>(IsActiveForConditional)
                              .WithInterfaces<IGetActiveUserIdentifier>(IsActiveForConditional)
                              .WithInterfaces<IGetPortalProvider>(IsActiveForConditional)
                              .FromNew<ModioMultiplatformAuthResolver>(SERVICE_BINDING_PRIORITY);

            
            _resolveUsingThis = true;
        }

        static bool IsActiveForConditional() => _resolveUsingThis;

        public Task<Error> Authenticate(bool displayedTerms, string thirdPartyEmail = null)
            => Get<IModioAuthService>().Authenticate(displayedTerms, thirdPartyEmail);

        public Task<string> GetActiveUserIdentifier() => Get<IGetActiveUserIdentifier>().GetActiveUserIdentifier();

        static T Get<T>()
        {
            if (ServiceOverride is T t) return t;
            
            _resolveUsingThis = false;
            var result = ModioServices.Resolve<T>(); 
            _resolveUsingThis = true;
            return result;
        }

        public bool IsEmailPlatform => Get<IModioAuthService>() is IPotentialModioEmailAuthService { IsEmailPlatform: true, };
        public ModioAPI.Portal Portal => Get<IGetPortalProvider>()?.Portal ?? ModioAPI.Portal.None;
    }
}
