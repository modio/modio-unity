using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Modio.API;
using Modio.API.SchemaDefinitions;
using Modio.Caching;
using Modio.Errors;
using Modio.Extensions;
using Filter = Modio.API.ModioAPI.Dependencies.GetModDependenciesFilter;

namespace Modio.Mods
{
    public class ModDependencies
    {
        const int MAX_DEPTH = 5;
        
        public int Count
        {
            get
            {
                if (!HasDependencies) return 0;

                int count = _flattenedMods?.Count ?? _depthMap?.Sum(list => list.Count) ?? 0;
                //Fetch the dependencies if we don't have them. This will InvokeModUpdated, allowing the UI to fetch again
                if(count == 0 && _isFetchingDependencies == null) FetchDependencies().ForgetTaskSafely();
                return count;
            }
        }
        public bool HasDependencies { get; }
        TaskCompletionSource<Error> _isFetchingDependencies;
        public bool IsMapped => _depthMap != null;
        
        List<Mod>[] _depthMap;
        readonly Mod _dependent;
        List<Mod> _flattenedMods;

        internal ModDependencies(Mod dependent, bool hasDependencies)
        {
            HasDependencies = hasDependencies;
            _dependent = dependent;
        }

        /// <summary>
        /// Gets all dependencies of the dependent.
        /// </summary>
        /// <returns>
        /// <p>An asynchronous task that returns a tuple (<see cref="Error"/> error, <see cref="IReadOnlyList{Mod}"/> results), where:</p>
        /// <p><c>error</c> is the error encountered during the task (if any)</p>
        /// <p><c>result</c> is a readonly list of <see cref="Mod"/> dependencies.</p>
        /// </returns>
        public async Task<(Error error, IReadOnlyList<Mod> results)> GetAllDependencies()
        {
            if(!IsMapped)
            {
                Error error = await FetchDependencies();

                if (error) return (error, Array.Empty<Mod>());
            }

            return (Error.None, _flattenedMods);
        }

        async Task<Error> FetchDependencies()
        {
            if (!HasDependencies)
            {
                ModioLog.Warning?.Log($"Attempting to get dependencies when none exist for this mod!");
                return new Error(ErrorCode.BAD_PARAMETER);
            }

            if (_isFetchingDependencies != null) await _isFetchingDependencies.Task;
            
            if (IsMapped) 
                return Error.None;

            // We make a new map instead of mal-forming the stored map
            var newMap = new List<Mod>[MAX_DEPTH + 1];
            for (var depth = 0; depth < newMap.Length; depth++)
                newMap[depth] = new List<Mod>();
            
            Filter filter = ModioAPI.Dependencies.FilterGetModDependencies().Recursive(true);
            
            _isFetchingDependencies = new TaskCompletionSource<Error>();
            
            // Iterate through every result
            while (true)
            {
                (Error error, Pagination<ModDependenciesObject[]>? result) = await ModioAPI.Dependencies.GetModDependencies(_dependent.Id, filter);

                if (error)
                {
                    ModioLog.Error?.Log($"Error getting dependencies for mod {_dependent}: {error.GetMessage()}");
                    _isFetchingDependencies.SetResult(error);
                    _isFetchingDependencies = null;
                    return error;
                }
                
                foreach (ModDependenciesObject dependency in result.Value.Data)
                {
                    if (dependency.Visible == 0)
                    {
                        // Rare edge case, unlikely to reach here but if we do we're unable to safely install the mod
                        ModioLog.Error?.Log($"Mod {_dependent} has incompatible dependency {dependency.Id}!");
                        return new Error(ErrorCode.INCOMPATIBLE_DEPENDENCIES);
                    }
                    
                    newMap[(int)dependency.DependencyDepth].Add(ModCache.GetMod(ConstructModObject(dependency)));
                }

                // Check for if we're on the last page, if not we increment the page number and iterate
                if (result.Value.ResultOffset + result.Value.ResultCount < result.Value.ResultTotal)
                    filter.PageIndex++;
                else
                    break;
            }

            _depthMap = newMap;
            
            _flattenedMods = new List<Mod>();
            for (var depth = MAX_DEPTH; depth >= 0; depth--)
                _flattenedMods.AddRange(_depthMap[depth]);
            _flattenedMods = _flattenedMods.Distinct().ToList();
            
            _isFetchingDependencies.SetResult(Error.None);
            _isFetchingDependencies = null;

            _dependent.InvokeModUpdated(ModChangeType.Dependencies);

            return Error.None;
        }
        
        static ModObject ConstructModObject(ModDependenciesObject dependency) => 
            new ModObject(dependency.Id, 
                dependency.GameId,
                dependency.Status,
                dependency.Visible,
                dependency.SubmittedBy,
                dependency.DateAdded,
                dependency.DateUpdated,
                dependency.DateLive,
                dependency.MaturityOption,
                dependency.CommunityOptions,
                dependency.MonetizationOptions,
                0L,
                dependency.Stock,
                dependency.Price,
                dependency.Tax,
                dependency.Logo,
                dependency.HomepageUrl,
                dependency.Name,
                dependency.NameId,
                dependency.Summary,
                dependency.Description,
                dependency.DescriptionPlaintext,
                dependency.MetadataBlob,
                dependency.ProfileUrl,
                dependency.Media,
                dependency.Modfile,
                dependency.Dependencies,
                dependency.Platforms,
                dependency.MetadataKvp,
                dependency.Tags,
                dependency.Stats
                );
    }
}
