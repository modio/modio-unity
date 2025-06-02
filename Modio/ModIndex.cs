using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Modio.API;
using Modio.API.SchemaDefinitions;
using Modio.Caching;
using Modio.Errors;
using Modio.Mods;
using Modio.Users;
using Newtonsoft.Json;

namespace Modio
{
    [System.Serializable]
    public class ModIndex
    {
        [JsonProperty]
        internal Dictionary<long, IndexEntry> Index = new Dictionary<long, IndexEntry>();

        [JsonProperty]
        internal Dictionary<long, ModObject> ModObjectCache = new Dictionary<long, ModObject>();

        [JsonIgnore]
        public bool IsDirty { get; internal set; }

        public ModIndex()
        {
            Mod.AddChangeListener(ModChangeType.ModObject, OnModObjectUpdate);
        }

        internal static async Task<(Error error, ModIndex result)> CreateIndexFromScan()
        {
            var output = new ModIndex();

            var allUserFiles = await ModioClient.DataStorage.ReadAllSavedUserData();

            if (!allUserFiles.error)
            {
                foreach (UserSaveObject user in allUserFiles.results)
                {
                    foreach (long modId in user.SubscribedMods)
                        if (output.Index.TryGetValue(modId, out IndexEntry entry)
                            && !entry.Subscribers.Contains(user.UserId))
                            entry.Subscribers.Add(user.UserId);
                        else 
                            output.Index[modId] = new IndexEntry{ Subscribers = new List<long>{ user.UserId, }, };
                }
            }

            var scannedModfiles = await ModioClient.DataStorage.ScanForModfiles();

            if (!scannedModfiles.error)
            {
                foreach ((long modId, long modfileId) in scannedModfiles.results)
                {
                    if (output.Index.TryGetValue(modId, out IndexEntry entry))
                    {
                        entry.DownloadedModfileId = modfileId;
                    }
                    else
                        output.Index[modId] = new IndexEntry
                        {
                            DownloadedModfileId = modfileId,
                            FileState = ModFileState.Downloaded,
                        };
                }
            }

            var scannedInstalls = await ModioClient.DataStorage.ScanForInstalledMods();

            if (!scannedInstalls.error)
            {
                foreach ((long modId, long modfileId) in scannedInstalls.results)
                {
                    if (output.Index.TryGetValue(modId, out IndexEntry entry))
                    {
                        entry.InstalledModfileId = modfileId;
                    }
                    else
                        output.Index[modId] = new IndexEntry
                        {
                            InstalledModfileId = modfileId,
                            FileState = ModFileState.Installed,
                        };
                }
            }

            (Error error, IEnumerable<Mod> mods) = await Mod.GetMods(output.Index.Keys);

            if (!error)
                foreach (Mod mod in mods)
                    output.ModObjectCache[mod.Id] = mod.LastModObject;

            return (Error.None, output);
        }

        internal async Task<bool> UpdateIndexWithMissingEntriesFromScan()
        {
            var scannedInstalls = await ModioClient.DataStorage.ScanForInstalledMods();
            
            List<long> missingModIds = null;

            if (!scannedInstalls.error)
            {
                foreach ((long modId, long modfileId) in scannedInstalls.results)
                {
                    if (Index.TryGetValue(modId, out IndexEntry entry))
                    {
                        if (entry.InstalledModfileId != 0)
                            continue;

                        entry.InstalledModfileId = modfileId;
                        entry.FileState = ModFileState.Installed;
                        IsDirty = true;
                    }
                    else
                    {
                        Index[modId] = new IndexEntry
                        {
                            InstalledModfileId = modfileId,
                            FileState = ModFileState.Installed,
                        };

                        missingModIds ??= new List<long>();
                        missingModIds.Add(modId);
                    }
                }
            }

            if (missingModIds == null)
                return false;

            (Error error, IEnumerable<Mod> mods) = await Mod.GetMods(missingModIds);

            if (!error)
                foreach (Mod mod in mods)
                    ModObjectCache[mod.Id] = mod.LastModObject;
            IsDirty = true;

            return true;
        }

        [System.Serializable]
        internal class IndexEntry
        {
            public const long ID_NONE = -1;
            public long DownloadedModfileId = ID_NONE;
            public long InstalledModfileId = ID_NONE;
            public long InstallationSize = 0;
            public List<long> Subscribers = new List<long>();
            public DateTime ExpiresAfter = DateTime.UnixEpoch;
            
            public ModFileState FileState;
        }

        internal IndexEntry GetEntry(Mod mod)
        {
            if (Index.TryGetValue(mod.Id, out IndexEntry indexEntry)) return indexEntry;

            indexEntry = new IndexEntry();
            Index[mod.Id] = indexEntry;
            ModObjectCache[mod.Id] = mod.LastModObject;
            mod.Logo?.CacheLowestResolutionOnDisk(true);
            
            return indexEntry;
        }

        internal bool TryGetEntry(ModId modId, out IndexEntry entry) => Index.TryGetValue(modId, out entry);

        internal void RemoveEntry(Mod mod)
        {
            Index.Remove(mod.Id);
            ModObjectCache.Remove(mod.Id);
            
            //remove the cached logo given the mod is no longer around
            mod.Logo?.CacheLowestResolutionOnDisk(false);
        }

        internal void Shutdown()
        {
            Mod.RemoveChangeListener(ModChangeType.ModObject, OnModObjectUpdate);
        }
        
        void OnModObjectUpdate(Mod mod, ModChangeType changeType)
        {
            if (!TryGetEntry(mod.Id, out IndexEntry entry)) return;

            ModObjectCache[mod.Id] = mod.LastModObject;
            mod.Logo?.CacheLowestResolutionOnDisk(true);
        }
    }
}
