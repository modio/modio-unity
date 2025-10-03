using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Modio.API;

namespace Modio.Authentication
{
    public class ModioMultiplatformAuthResolver : IModioAuthService, 
                                                  IGetActiveUserIdentifier, 
                                                  IPotentialModioEmailAuthService
    {
        protected const ModioServicePriority SERVICE_BINDING_PRIORITY = ModioServicePriority.PlatformProvided + 9;
        bool _resolveUsingThis;

        public IModioAuthService ServiceOverride { get; set; }
        public IReadOnlyList<IModioAuthService> AuthBindings { get; private set; }

        public ModioMultiplatformAuthResolver()
        {
            GetAllBindings();
            
            ModioServices.Bind<ModioMultiplatformAuthResolver>()
                         .WithInterfaces<IModioAuthService>(IsActiveForConditional)
                         .WithInterfaces<IGetActiveUserIdentifier>(IsActiveForConditional)
                         .FromInstance(this, SERVICE_BINDING_PRIORITY);
            
            ModioServices.AddBindingChangedListener<IModioAuthService>(OnAuthServiceBound);
            
            _resolveUsingThis = true;
        }

        ~ModioMultiplatformAuthResolver() 
            => ModioServices.RemoveBindingChangedListener<IModioAuthService>(OnAuthServiceBound);

        void OnAuthServiceBound(IModioAuthService _) => GetAllBindings();

        void GetAllBindings()
        {
            AuthBindings = ModioServices.GetBindings<IModioAuthService>()
                                        .ResolveAll()
                                        .OrderByDescending(platformPair => platformPair.priority)
                                        .Select(platformPair => platformPair.service)
                                        .Where(platform => platform is IGetActiveUserIdentifier)
                                        .Where(service => service is not ModioMultiplatformAuthResolver)
                                        .ToList();

            if (ServiceOverride is null
                || !AuthBindings.Contains(ServiceOverride))
                ServiceOverride = AuthBindings.FirstOrDefault();
        }

        bool IsActiveForConditional() => _resolveUsingThis;

        public Task<Error> Authenticate(bool displayedTerms, string thirdPartyEmail = null, bool sync = true)
            => Get<IModioAuthService>().Authenticate(displayedTerms, thirdPartyEmail, sync);

        public Task<string> GetActiveUserIdentifier() => Get<IGetActiveUserIdentifier>().GetActiveUserIdentifier();

        protected T Get<T>()
        {
            if (ServiceOverride is T t) return t;
            
            _resolveUsingThis = false;
            var result = ModioServices.Resolve<T>(); 
            _resolveUsingThis = true;
            return result;
        }

        public bool IsEmailPlatform => Get<IModioAuthService>() is IPotentialModioEmailAuthService { IsEmailPlatform: true, };
        public ModioAPI.Portal Portal => Get<IModioAuthService>()?.Portal ?? ModioAPI.Portal.None;
    }
}
