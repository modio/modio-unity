using System;
using System.Threading.Tasks;
using Modio.API;
using Modio.API.SchemaDefinitions;

namespace Modio.Mods
{
    public class ModPlatform
    {
        public readonly ModioAPI.Platform Platform;
        public readonly long ModfileLiveId;
        
        readonly long _modId;
        Modfile _modfile;

        internal ModPlatform(ModPlatformsObject platformObject, long modId)
        {
            _modId = modId;
            Platform = ModioAPI.PlatformFromHeader(platformObject.Platform);
            ModfileLiveId = platformObject.ModfileLive;
        }

        public async Task<(Error, Modfile)> GetModfile()
        {
            if(_modfile != null) return (Error.None, _modfile);
            
            (Error error, ModfileObject? modfileObject) = await ModioAPI.Files.GetModfile(_modId, ModfileLiveId);

            if(error) return (error, null);

            _modfile = new Modfile(modfileObject.Value);
            return (error, _modfile);
        }
    }
}
