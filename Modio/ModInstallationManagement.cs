using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Modio.API;
using Modio.API.SchemaDefinitions;
using Modio.Caching;
using Modio.Errors;
using Modio.Mods;
using Modio.Collections;
using Modio.Extensions;
using Modio.Settings;
using Modio.Users;

namespace Modio
{
    /// <summary>
    /// Manages mod downloading, installation, updating & uninstallation.
    /// </summary>
    public static class ModInstallationManagement
    {
        /// <summary>
        /// If set to true plugin will attempt to download and extract in one step.
        /// </summary>
        public static bool DownloadAndExtractAsSingleJob { get; set; } = true;

        /// <summary>
        /// Returns the current mod that the current operation is running on.
        /// </summary>
        public static Mod CurrentOperationOnMod => _currentOperation?.Mod;

        internal static ModIndex _index;

        static Job _currentOperation;
        static bool _isRunning;

        static Queue<Job> _operationQueue;

        static HashSet<ModId> _currentSessionMods = new HashSet<ModId>();
        static HashSet<Mod> _modsToUninstall = new HashSet<Mod>();
        static HashSet<Mod> _unverifiedMods = new HashSet<Mod>(); // present at session start and not yet checked
        static bool _hasScannedMissingMods;

        static bool _isDeactivated;

        internal static bool IsRunning => _isRunning;

        public delegate void InstallationManagementEventDelegate(
            Mod mod,
            Modfile modfile,
            OperationType jobType,
            OperationPhase jobPhase
        );
        
        public static event InstallationManagementEventDelegate ManagementEvents;
        
        /// <summary>
        /// Returns if the mod installation management is initialized.
        /// </summary>
        public static bool IsInitialized => _index != null;
        
        /// <summary>
        /// Returns the number of operations in queue does not include the current operation
        /// </summary>
        public static int PendingModOperationCount => _operationQueue?.Count ?? 0;

        
        internal static async Task<Error> Init()
        {
            (Error error, ModIndex index) = await ModioClient.DataStorage.ReadIndexData();
            if (error) (error, index) = await ModIndex.CreateIndexFromScan();

            _index = !error ? index : new ModIndex();

            _operationQueue = new Queue<Job>();
            
            // Reset in case of switching game
            _unverifiedMods.Clear();
            _hasScannedMissingMods = false;

            // Guaranteeing that the mods we will be referencing will have their data
            // The only team we can have this missing data is from reading/scanning the index
            await Mod.GetMods(_index.Index.Keys);

            foreach (KeyValuePair<long, ModIndex.IndexEntry> entryPair in _index.Index)
            {
                Mod mod = GetModRespectingIndexCache(entryPair.Key);

                ModIndex.IndexEntry entry = entryPair.Value;

                if (mod.File != null) // shouldn't be possible so long as the mod was in the index cache, but protect against corrupt index
                {
                    mod.File.State = entry.FileState;
                    if(mod.File.State == ModFileState.Installed)
                        mod.File.InstallLocation = ModioClient.DataStorage.GetInstallPath(mod.Id, entry.InstalledModfileId);
                    mod.InvokeModUpdated(ModChangeType.FileState);
                }

                mod.Logo?.CacheLowestResolutionOnDisk(true);

                // Don't want to override synced sub status here, only if we couldn't get them do we set
                if (!User.Current.ModRepository.HasGotSubscriptions)
                    mod.UpdateLocalSubscriptionStatus(entry.Subscribers.Contains(User.Current.Profile.UserId));
                
                _unverifiedMods.Add(mod);
            }

            Mod.AddChangeListener(ModChangeType.IsSubscribed, OnModSubscriptionChange);

            var settings = ModioServices.Resolve<ModioSettings>();
            
            // We want to check this if the settings are present, but if not ignore
            if (!settings.TryGetPlatformSettings(out ModInstallationManagementSettings managementSettings))
                Activate();
            else if (managementSettings.AutoActivate)
                Activate();
            else
                _isDeactivated = true;
            
            return error;
        }

        internal static async Task Shutdown()
        {
            Mod.RemoveChangeListener(ModChangeType.IsSubscribed, OnModSubscriptionChange);

            _index?.Shutdown();
            _index = null;

            while (_currentOperation != null) await Task.Yield();
        }

        internal static void WakeUp()
        {
            // If it's not initialized, this will be called again in Init
            if (_index == null) return;
            ExecuteJobs();
        }

        /// <summary>Begin downloading and installing mods.</summary>
        public static void Activate()
        {
            _isDeactivated = false;
            WakeUp();
        }

        /// <summary>Stop downloading and installing mods.</summary>
        /// <remarks>Uninstallations will also be stopped.</remarks>
        public static void Deactivate(bool cancelCurrentJob)
        {
            // Nap time
            _isDeactivated = true;
            _operationQueue.Clear();
            
            if (cancelCurrentJob) 
                CancelInstallOperation(CurrentOperationOnMod);
        }

        /// <summary>
        /// Returns a boolean indicating if a given mod is subscribed by the given user
        /// </summary>
        /// <param name="modId">The mod id</param>
        /// <param name="userId">The user id</param>
        /// <returns>True if the mod is subscribed by the given user.</returns>
        public static bool IsModSubscribed(long modId, long userId)
            => IsInitialized &&
               _index.Index.TryGetValue(modId, out ModIndex.IndexEntry entry) &&
               entry.Subscribers.Contains(userId);

        
        /// <summary>
        /// Returns a collection of all installed mods.
        /// </summary>
        /// <returns>The collection of mods</returns>
        public static ICollection<Mod> GetAllInstalledMods()
            => IsInitialized
                // We move to an extra array to avoid modification problems
                ? _index.ModObjectCache.Values.ToArray().Select(ModCache.GetMod).ToArray()
                : (ICollection<Mod>)Array.Empty<Mod>();

        /// <summary>
        /// Returns the total disk usage in bytes
        /// </summary>
        /// <param name="includeQueued">
        /// Wether to include queued (pending) operation, if true
        /// will account for file IO changes to be made by the queued operation</param>
        /// <returns>The total disk usage (bytes)</returns>
        public static long GetTotalDiskUsage(bool includeQueued)
        {
            if (_index?.Index == null) return 0;

            long totalDiskUsage = 0;

            foreach ((long modId, ModIndex.IndexEntry _) in _index.Index)
            {
                Mod mod = GetModRespectingIndexCache(modId);
                
                if (mod?.File == null) continue;
                if (mod.File.State == ModFileState.FileOperationFailed) continue;
                
                long fileSize = mod.File.FileSize;

                if (!includeQueued)
                {
                    //approximate disk usage for partially installed mods
                    if (mod.File.State == ModFileState.Downloading || mod.File.State == ModFileState.Installing)
                        fileSize = (long)(fileSize * mod.File.FileStateProgress);
                    else if (mod.File.State != ModFileState.Installed) continue;
                }

                totalDiskUsage += fileSize;
            }

            return totalDiskUsage;
        }

        static Mod GetModRespectingIndexCache(long modId)
        {
            if (ModCache.TryGetMod(modId, out Mod mod))
                return mod;

            if (_index.ModObjectCache.TryGetValue(modId, out ModObject modObject))
                return ModCache.GetMod(modObject);

            return Mod.Get(modId);
        }

        static async Task<Error> SaveIndex()
        {
            _index.IsDirty = false;
            return await ModioClient.DataStorage.WriteIndexData(_index);
        }

        static async void ExecuteJobs()
        {
            if (_isRunning)
            {
                //We're already running, don't run it twice. But do update our jobs, so they can show correct pending state

                await EnqueueJobs();
                return;
            }

            _isRunning = true;

            try
            {
                while (ModioClient.IsInitialized || ModioClient.IsCurrentlyInitializing)
                {
                    //catch a rare edge case when doing a shutdown/init
                    if(_index == null) break;
                    
                    await EnqueueJobs();

                    if (_index.IsDirty)
                        await SaveIndex();
                    else if (!_operationQueue.Any()) //else as if we awaited a save, we need to check for new jobs after
                    {
                        _isRunning = false;
                        return;
                    }

                    try
                    {
                        while (_operationQueue.Any() && _index != null)
                        {
                            _currentOperation = _operationQueue.Dequeue();

                            ModioLog.Verbose?.Log($"Executing MIM operation: {_currentOperation}");
                            
                            Error error = await _currentOperation.Run();

                            if (error)
                                if (error.IsSilent)
                                    ModioLog.Verbose?.Log(
                                        $"Cancelled performing {_currentOperation.Type} job for mod {_currentOperation.Mod}: {error}"
                                    );
                                else
                                    ModioLog.Error?.Log(
                                        $"Error performing {_currentOperation.Type} job for mod {_currentOperation.Mod}: {error}"
                                    );

                            if (!ModioClient.IsInitialized && !ModioClient.IsCurrentlyInitializing)
                            {
                                _currentOperation = null;
                                _isRunning = false;
                                return;
                            }

                            if (_index.IsDirty)
                                await SaveIndex();
                        }
                    }
                    finally
                    {
                        _currentOperation = null;
                    }

                    await Task.Yield(); //prevent this becoming an infinite loop if something goes wrong
                }
            }
            finally
            {
                _isRunning = false;
            }
        }

        static Task EnqueueJobs()
        {
            _operationQueue.Clear();

            foreach (Mod mod in User.Current.ModRepository.GetSubscribed())
            {
                //If the modfile is missing, this is likely a saved ModID and not much else
                if (mod.File == null) continue;

                _index.GetEntry(mod);
            }

            foreach (KeyValuePair<long, ModIndex.IndexEntry> entry in _index.Index)
            {
                var modId = new ModId(entry.Key);

                bool localUserIsSubscribed = User.Current.ModRepository.IsSubscribed(modId);

                var subscriberList = entry.Value.Subscribers;

                if (localUserIsSubscribed && !subscriberList.Contains(User.Current.UserId))
                {
                    subscriberList.Add(User.Current.Profile.UserId);
                    _index.IsDirty = true;
                }
                else if (!localUserIsSubscribed && subscriberList.Contains(User.Current.UserId))
                {
                    subscriberList.Remove(User.Current.Profile.UserId);
                    _index.IsDirty = true;
                }

                bool anyOtherSubscribers = localUserIsSubscribed || subscriberList.Count > 0;

                bool tempModIsValid = entry.Value.ExpiresAfter > DateTime.Today.ToUniversalTime() ||
                                      _currentSessionMods.Contains(modId);

                Mod mod = GetModRespectingIndexCache(modId);

                // By early outing here, we apply index changes without installing/uninstalling anything
                if (_isDeactivated) continue;
                if (mod.File == null) continue;

                //don't retry a failed file operation without external input, as we'll likely just fail again and get rate limited
                if (mod.File.State == ModFileState.FileOperationFailed)
                {
                    // If no one wants it, we can reset it
                    if (!anyOtherSubscribers)
                        mod.File.State = ModFileState.None;

                    continue;
                }

                if ((!anyOtherSubscribers && !tempModIsValid)
                    || (!tempModIsValid && !localUserIsSubscribed && mod.File.State is not ModFileState.Installed and not ModFileState.Uninstalling)
                    || _modsToUninstall.Contains(mod))
                    _operationQueue.Enqueue(new UninstallJob(mod));
                else
                    EnqueueJobsIfNeeded(entry.Value, mod);
            }
            
            if(!_hasScannedMissingMods)
                _operationQueue.Enqueue(new ScanMissingInstallsJob());

            return Task.CompletedTask;

            void EnqueueJobsIfNeeded(ModIndex.IndexEntry entry, Mod mod)
            {
                if (_unverifiedMods.Contains(mod))
                {
                    _operationQueue.Enqueue(new ValidateJob(mod));
                }

                if (mod.File == null
                    || mod.File.State == ModFileState.Installing 
                    || mod.File.State == ModFileState.Updating
                    || mod.File.State == ModFileState.FileOperationFailed
                    || entry.InstalledModfileId == mod.File.Id)
                    return;

                bool isUpdateJob = entry.InstalledModfileId != ModIndex.IndexEntry.ID_NONE;

                if (DownloadAndExtractAsSingleJob)
                {
                    if(mod.File.State != ModFileState.Downloading)                        
                        _operationQueue.Enqueue(new DownloadAndExtractJob(mod, isUpdateJob));
                }
                else
                {
                    if ((entry.DownloadedModfileId != mod.File.Id || !ModioClient.DataStorage.DoesModfileExist(mod.Id, mod.File.Id))
                        && mod.File.State != ModFileState.Downloading)
                        _operationQueue.Enqueue(new DownloadJob(mod));

                    _operationQueue.Enqueue(new InstallJob(mod, isUpdateJob));
                }

                if (mod.File.State == ModFileState.None)
                {
                    mod.File.State = ModFileState.Queued;
                    mod.InvokeModUpdated(ModChangeType.FileState);
                }
            }
        }

        [ModioDebugMenu(ShowInSettingsMenu = false)]
        static string TempMods { get; set; }
        [ModioDebugMenu(ShowInSettingsMenu = false)]
        static bool TempModsAppendCurrentSession { get; set; }

        [ModioDebugMenu(ShowInSettingsMenu = false)]
        static void StartTempModSession()
        {
            List<ModId> modIds = TempMods?.Split(',').Select(s =>
            {
                if (long.TryParse(s.Trim(), out long modId))
                    return new ModId(modId);

                ModioLog.Error?.Log($"Couldn't parse {s} to a modId. Please use comma separated ID numbers");
                return default(ModId);
            }).ToList();

            if (modIds == null || modIds.Count == 0)
            {
                ModioLog.Error?.Log($"Couldn't parse modIds. Please use comma separated ID numbers");
                return;
            }
            
            StartTempModSession(modIds, TempModsAppendCurrentSession).ForgetTaskSafely();
        }

        
        /// <summary>
        /// Will start a temporary mod session.
        /// </summary>
        /// <param name="tempMods">A list of mods to be used for the temp session.</param>
        /// <param name="appendCurrentSession">Whether the new mods should be added to the existing running mod session.</param>
        /// <returns>
        /// An asynchronous task that returns <see cref="Error"/>.<see cref="Error.None"/> on success.
        /// </returns>
        /// <seealso cref="EndCurrentTempModSession"/>
        public static async Task<Error> StartTempModSession(List<ModId> tempMods, bool appendCurrentSession = false)
        {
            if (_currentSessionMods.Count == 0 && !appendCurrentSession)
            {
                ModioLog.Message?.Log(
                    "Attempting to start new Temp Mod Session while one is active! Ending " +
                    "previous session. If you want to append mods onto the current session, " +
                    "set appendCurrentSession to true."
                );

                EndCurrentTempModSession();
            }

            foreach (ModId mod in tempMods) _currentSessionMods.Add(mod);

            return await AddTemporaryMods(tempMods, 0);
        }

        /// <summary>
        /// Will end the current temp mod session started by <see cref="StartTempModSession"/>
        /// </summary>
        [ModioDebugMenu(ShowInSettingsMenu = false)]
        public static void EndCurrentTempModSession()
        {
            _currentSessionMods.Clear();

            ExecuteJobs();
        }

        /// <summary>
        /// Adds the list of mods to mod index
        /// </summary>
        /// <param name="tempMods">The list of mods to be added</param>
        /// <param name="lifeTimeDaysOverride">The number of days to keep the mods installed</param>
        /// <returns>
        /// An asynchronous task that returns <see cref="Error"/>.<see cref="Error.None"/> on success.
        /// </returns>
        public static async Task<Error> AddTemporaryMods(List<ModId> tempMods, int lifeTimeDaysOverride = -1)
        {
            int lifeTimeDays = lifeTimeDaysOverride > -1
                ? lifeTimeDaysOverride
                : ModioClient.Settings.GetPlatformSettings<TempModInstallationSettings>().LifeTimeDays;

            List<long> longIds = tempMods.Select(modId => (long)modId).ToList();

            (Error error, ICollection<Mod> mods) = await Mod.GetMods(longIds);

            if (error) return error;
            
            if (tempMods.Count != mods.Count && tempMods.Distinct().Count() != mods.Count)
            {
                //oh no
                ModioLog.Warning?.Log($"AddTemporaryMods failed; only able to fetch {mods.Count} temporary mods. Expected {tempMods.Distinct().Count()}");
                return new Error(ErrorCode.REQUESTED_MODFILE_NOT_FOUND);
            }

            // If one mod is tainted the whole session is missing a dependency, so we early out
            bool canInstall = !tempMods.Any(
                modId =>
                {
                    if (!_index.TryGetEntry(modId, out ModIndex.IndexEntry entry)) return false;

                    return entry.FileState == ModFileState.FileOperationFailed;
                }
            );

            if (!canInstall) return new Error(ErrorCode.CANT_INSTALL_TAINTED_MOD);

            foreach (Mod mod in mods) AddTemporaryMod(mod, lifeTimeDays);

            return Error.None;
        }

        /// <summary>
        /// Adds a mod to mod index
        /// </summary>
        /// <param name="modId">The mod to be added</param>
        /// <param name="lifetime">The number of days to keep the mod installed</param>
        static void AddTemporaryMod(Mod modId, int lifetime)
        {
            ModIndex.IndexEntry indexEntry = _index.GetEntry(modId);

            switch (indexEntry.FileState)
            {
                case ModFileState.Uninstalling:
                case ModFileState.None:
                    // Today is a local time, but we compare in UTC, thus we must ensure we get
                    // the universal time to get UTC today
                    indexEntry.ExpiresAfter = lifetime == 0
                        ? DateTime.UnixEpoch
                        : DateTime.Today.ToUniversalTime().AddDays(lifetime);

                    WakeUp();
                    break;

                // Mods already being/been installed, safe to ignore
                case ModFileState.Downloaded:
                case ModFileState.Downloading:
                case ModFileState.Installed:
                case ModFileState.Installing:
                case ModFileState.Updating:
                case ModFileState.Queued:
                    // Since this was set a lifetime, we want to respect it if this request is only for the session
                    indexEntry.ExpiresAfter = lifetime == 0
                        ? indexEntry.ExpiresAfter
                        : DateTime.Today.ToUniversalTime().AddDays(lifetime);

                    break;

                case ModFileState.FileOperationFailed:
                default:
                    break;
            }
        }

        // EnqueueJobs will check for any expired mods and queue uninstallation

        public static void ClearExpiredTempMods() => ExecuteJobs();

        static void RetryInstallingTaintedMods()
        {
            foreach (KeyValuePair<long, ModIndex.IndexEntry> entry in _index.Index)
            {
                Mod mod = null;

                if (entry.Value.FileState != ModFileState.FileOperationFailed)
                {
                    if (entry.Value.FileState != ModFileState.None)
                        continue;

                    mod = GetModRespectingIndexCache(entry.Key);
                    if (mod.File?.State != ModFileState.FileOperationFailed)
                        continue;
                }

                entry.Value.FileState = ModFileState.None;

                mod ??= GetModRespectingIndexCache(entry.Key);

                mod.File.State = ModFileState.None;
                mod.InvokeModUpdated(ModChangeType.FileState);
            }

            WakeUp();
        }

        /// <summary>
        /// Retries installing a given mod.
        /// </summary>
        /// <param name="mod">The Mod to be installed.</param>
        public static async Task<Error> RetryInstallingMod(Mod mod)
        {
            if (mod.File.State != ModFileState.FileOperationFailed) 
                return Error.None;

            _index.GetEntry(mod).FileState = ModFileState.None;
            mod.File.State = ModFileState.None;
            mod.InvokeModUpdated(ModChangeType.FileState);
            
            bool spaceAvailable = await IsThereAvailableSpaceFor(mod);
            
            if (!spaceAvailable)
            {
                var error = new FilesystemError(FilesystemErrorCode.INSUFFICIENT_SPACE);

                mod.File.FileStateErrorCause = error;
                mod.File.State = ModFileState.FileOperationFailed;

                mod.InvokeModUpdated(ModChangeType.FileState);

                return error;
            }

            WakeUp();
            return Error.None;
        }

        /// <summary>
        /// Marks a mod for uninstallation.
        /// </summary>
        /// <param name="mod">The Mod to be uninstalled.</param>
        public static void MarkModForUninstallation(Mod mod)
        {
            _modsToUninstall.Add(mod);
            WakeUp();
        }

        static void OnModSubscriptionChange(Mod mod, ModChangeType changeType)
        {
            if (ModChangeType.IsSubscribed == changeType &&
                !mod.IsSubscribed)
            {
                CancelInstallOperation(mod);
            }

            if (!mod.IsSubscribed || !_modsToUninstall.Contains(mod)) return;

            // Safe to just remove here, uninstall operation is too fast to realistically cancel
            _modsToUninstall.Remove(mod);

            WakeUp();
        }

        static void CancelInstallOperation(Mod mod)
        {
            if (_currentOperation == null) return;

            if (_currentOperation.Mod != mod) return;

            if (_currentOperation.Type == OperationType.Download ||
                _currentOperation.Type == OperationType.Install)
                _currentOperation.Cancel();
        }

#region Jobs

        abstract class Job
        {
            readonly CancellationTokenSource _cancellationTokenSource;
            public Func<Task<Error>> Operation;
            public readonly OperationType Type;
            public readonly Mod Mod;
            protected readonly CancellationToken CancellationToken;
            protected OperationPhase Phase;

            protected Job(Mod mod, OperationType type)
            {
                Type = type;
                Mod = mod;
                _cancellationTokenSource = new CancellationTokenSource();
                CancellationToken = _cancellationTokenSource.Token;
            }

            public abstract Task<Error> Run();

            protected void PostEvent(OperationPhase jobPhase, ModFileState modState, Error errorCause = null)
            {
                if (Mod.File.State != modState)
                {
                    Mod.File.State = modState;
                    Mod.File.FileStateErrorCause = errorCause ?? Error.None;
                    Mod.InvokeModUpdated(ModChangeType.FileState);
                }

                Phase = jobPhase;
                ManagementEvents?.Invoke(Mod, Mod.File, Type, jobPhase);
            }

            internal void Cancel()
            {
                _cancellationTokenSource.Cancel();
            }

            internal abstract void GetPendingSpaceChange(ref long spaceRequired, ref long tempSpaceRequired);

            public override string ToString() => $"{Type} {Mod}: {Phase}";
        }

        class DownloadJob : Job
        {
            public DownloadJob(Mod mod) : base(mod, OperationType.Download) { }

            public override async Task<Error> Run()
            {
                PostEvent(OperationPhase.Checking, ModFileState.Downloading);

                Error error;

                // Cached Modfiles can expire, so we need to validate our cached URL and replace it if needed
                if (Mod.File.Download.ExpiresAfter < DateTime.Now.ToUniversalTime())
                {
                    ModioLog.Verbose?.Log($"Cached Modfile download for Mod {Mod.Name} has expired, getting new download");
                    
                    ModfileObject? modfileObjectNullable;
                    (error, modfileObjectNullable) = await ModioAPI.Files.GetModfile(Mod.Id, Mod.File.Id);

                    if (error)
                    {
                        PostEvent(OperationPhase.Failed, ModFileState.FileOperationFailed, error);
                        return error;
                    }

                    if (modfileObjectNullable != null)
                    {
                        ModfileObject modfileObject = modfileObjectNullable.Value;
                        Mod.UpdateModfile(new Modfile(modfileObject));
                    }
                    else
                        return new Error(ErrorCode.NO_DATA_AVAILABLE);
                }

                if (ModioClient.DataStorage.DoesModfileExist(Mod.Id, Mod.File.Id))
                {
                    PostEvent(OperationPhase.Completed, Mod.File.State);
                    return Error.None;
                }

                string downloadBinaryUrl = Mod.File.Download.BinaryUrl;
                long modfileId = Mod.File.Id;
                string filehashMd5 = Mod.File.Md5Hash;

                bool hasSpace = await ModioClient.DataStorage.IsThereAvailableFreeSpaceFor(
                    Mod.File.ArchiveFileSize,
                    Mod.File.FileSize
                );

                if (!hasSpace)
                {
                    error = new Error(ErrorCode.INSUFFICIENT_SPACE);
                    PostEvent(OperationPhase.Failed, ModFileState.FileOperationFailed, error);
                    return error;
                }

                PostEvent(OperationPhase.Started, ModFileState.Downloading);

                Stream stream;

                (error, stream) = await ModioClient.Api.DownloadFile(downloadBinaryUrl, CancellationToken);

                if (error)
                {
                    if (error.Code == ErrorCode.OPERATION_CANCELLED)
                        PostEvent(OperationPhase.Cancelled, ModFileState.None);
                    else 
                        PostEvent(OperationPhase.Failed, ModFileState.FileOperationFailed, error);
                    return error;
                }

                Task<Error> downloadTask = ModioClient.DataStorage.DownloadModFileFromStream(
                    Mod.Id,
                    modfileId,
                    stream,
                    filehashMd5,
                    CancellationToken
                );

                if (CancellationToken.IsCancellationRequested)
                {
                    await stream.DisposeAsync();
                    PostEvent(OperationPhase.Cancelled, ModFileState.None);
                    return new Error(ErrorCode.OPERATION_CANCELLED);
                }

                error = await downloadTask;

                if (error)
                {
                    if(error.Code == ErrorCode.OPERATION_CANCELLED)
                        PostEvent(OperationPhase.Cancelled, ModFileState.None);
                    else
                        PostEvent(OperationPhase.Failed, ModFileState.FileOperationFailed, error);
                    return error;
                }

                if (_index == null) // From Shutdown
                {
                    PostEvent(OperationPhase.Cancelled, ModFileState.None);
                    return new Error(ErrorCode.SHUTTING_DOWN);
                }

                _index.GetEntry(Mod).FileState = ModFileState.Downloaded;
                _index.GetEntry(Mod).DownloadedModfileId = modfileId;
                await SaveIndex();

                PostEvent(OperationPhase.Completed, ModFileState.Downloaded);

                return error;
            }

            internal override void GetPendingSpaceChange(ref long spaceRequired, ref long tempSpaceRequired)
            {
                switch (Phase)
                {
                    case OperationPhase.Checking:
                        tempSpaceRequired += Mod.File.ArchiveFileSize;
                        break;

                    case OperationPhase.Started:
                        tempSpaceRequired += (long)(Mod.File.ArchiveFileSize * (1 - Mod.File.FileStateProgress));
                        break;
                }
            }
        }

        class InstallJob : Job
        {
            readonly bool _isUpdateJob;

            public InstallJob(Mod mod, bool isUpdateJob) : base(
                mod,
                isUpdateJob ? OperationType.Update : OperationType.Install
            ) => _isUpdateJob = isUpdateJob;

            public override async Task<Error> Run()
            {
                if (Mod.File.State is ModFileState.FileOperationFailed or ModFileState.None)
                {
                    //Early out; installing isn't valid when modfile downloading failed
                    
                    return new Error(ErrorCode.OPERATION_CANCELLED);
                }
                
                ModFileState progressFileState = _isUpdateJob ? ModFileState.Updating : ModFileState.Installing;

                PostEvent(OperationPhase.Checking, progressFileState);

                Mod.File.FileStateProgress = 0;
                Mod.File.DownloadingBytesPerSecond = 0;
                Mod.InvokeModUpdated(ModChangeType.DownloadProgress);

                if (ModioClient.DataStorage.DoesInstallExist(Mod.Id, Mod.File.Id))
                {
                    _index.GetEntry(Mod).FileState = ModFileState.Installed;
                    _index.GetEntry(Mod).InstalledModfileId = Mod.File.Id;
                    await SaveIndex();

                    PostEvent(OperationPhase.Completed, ModFileState.Installed);
                    return Error.None;
                }

                if (!ModioClient.DataStorage.DoesModfileExist(Mod.Id, Mod.File.Id))
                {
                    ModioLog.Error?.Log(
                        $"Unable to install mod {Mod.Id}_{Mod.File.Id} as Modfile could not be found!"
                    );

                    PostEvent(OperationPhase.Failed, ModFileState.FileOperationFailed);
                    //SUB_MANAGEMENT_MODFILE_DOESNT_EXIST
                    return new Error(ErrorCode.FILE_NOT_FOUND);
                }

                long modfileFileSize = Mod.File.FileSize;

                if (_isUpdateJob)
                {
                    long previousInstallSize = _index.GetEntry(Mod).InstallationSize;
                    modfileFileSize -= previousInstallSize;
                }

                bool hasSpace = modfileFileSize > 0 &&
                                await ModioClient.DataStorage.IsThereAvailableFreeSpaceForModInstall(modfileFileSize);

                Error error;

                if (!hasSpace)
                {
                    error = new Error(ErrorCode.INSUFFICIENT_SPACE);
                    PostEvent(OperationPhase.Failed, ModFileState.FileOperationFailed, error);
                    return error;
                }

                PostEvent(OperationPhase.Started, progressFileState);

                Mod.File.State = progressFileState;
                Mod.InvokeModUpdated(ModChangeType.FileState);

                if (_isUpdateJob)
                {
                    error = await ModioClient.DataStorage.DeleteInstalledMod(
                        Mod,
                        _index.GetEntry(Mod).InstalledModfileId
                    );

                    if (error)
                    {
                        PostEvent(OperationPhase.Failed, ModFileState.FileOperationFailed, error);
                        return error;
                    }
                }

                error = await ModioClient.DataStorage.InstallMod(Mod, Mod.File.Id, CancellationToken);

                if (error.Code == ErrorCode.OPERATION_CANCELLED)
                {
                    PostEvent(OperationPhase.Cancelled, ModFileState.None);
                    return new Error(ErrorCode.OPERATION_CANCELLED);
                }

                if (error)
                {
                    PostEvent(OperationPhase.Failed, ModFileState.FileOperationFailed, error);
                    return error;
                }

                Mod.File.InstallLocation = ModioClient.DataStorage.GetInstallPath(Mod.Id, Mod.File.Id);

                if (_index == null) // From Shutdown
                {
                    PostEvent(OperationPhase.Cancelled, ModFileState.None);
                    return new Error(ErrorCode.SHUTTING_DOWN);
                }
                
                _index.GetEntry(Mod).FileState = ModFileState.Installed;
                _index.GetEntry(Mod).InstalledModfileId = Mod.File.Id;
                await SaveIndex();

                PostEvent(OperationPhase.Completed, ModFileState.Installed);
                return Error.None;
            }

            internal override void GetPendingSpaceChange(ref long spaceRequired, ref long tempSpaceRequired)
            {
                tempSpaceRequired -= Mod.File.ArchiveFileSize;

                switch (Phase)
                {
                    case OperationPhase.Checking:
                        spaceRequired += Mod.File.FileSize;
                        if (_isUpdateJob) spaceRequired -= _index.GetEntry(Mod).InstallationSize;
                        break;

                    case OperationPhase.Started:
                    {
                        spaceRequired += (long)(Mod.File.FileSize * (1.0 - Mod.File.FileStateProgress));

                        if (_isUpdateJob && Mod.File.FileStateProgress == 0f)
                            spaceRequired -= _index.GetEntry(Mod).InstallationSize;
                        break;
                    }
                }
            }
        }

        class DownloadAndExtractJob : Job
        {
            bool IsUpdateJob => Type == OperationType.Update;

            public DownloadAndExtractJob(Mod mod, bool isUpdateJob) : base(
                mod,
                isUpdateJob ? OperationType.Update : OperationType.Download
            ) { }

            public override async Task<Error> Run()
            {
                ModFileState progressFileState = IsUpdateJob ? ModFileState.Updating : ModFileState.Downloading;

                PostEvent(OperationPhase.Checking, progressFileState);

                Error error;

                // Cached Modfiles can expire, so we need to validate our cached URL and replace it if needed
                if (Mod.File.Download.ExpiresAfter < DateTime.Now.ToUniversalTime())
                {
                    ModioLog.Verbose?.Log($"Cached Modfile download for Mod {Mod.Name} has expired, getting new download");

                    ModfileObject? modfileObjectNullable;
                    (error, modfileObjectNullable) = await ModioAPI.Files.GetModfile(Mod.Id, Mod.File.Id);

                    if (error)
                    {
                        PostEvent(OperationPhase.Failed, ModFileState.FileOperationFailed, error);
                        return error;
                    }

                    if (modfileObjectNullable != null)
                    {
                        ModfileObject modfileObject = modfileObjectNullable.Value;
                        Mod.UpdateModfile(new Modfile(modfileObject));
                    }
                    else
                        return new Error(ErrorCode.NO_DATA_AVAILABLE);
                }
                
                if (ModioClient.DataStorage.DoesInstallExist(Mod.Id, Mod.File.Id))
                {
                    _index.GetEntry(Mod).FileState = ModFileState.Installed;
                    _index.GetEntry(Mod).InstalledModfileId = Mod.File.Id;
                    await SaveIndex();

                    PostEvent(OperationPhase.Completed, ModFileState.Installed);
                    return Error.None;
                }

                long modfileFileSize = Mod.File.FileSize;

                if (IsUpdateJob)
                {
                    long previousInstallSize = _index.GetEntry(Mod).InstallationSize;
                    modfileFileSize -= previousInstallSize;
                }

                bool hasSpace = modfileFileSize > 0 &&
                                await ModioClient.DataStorage.IsThereAvailableFreeSpaceForModInstall(modfileFileSize);

                if (!hasSpace)
                {
                    error = new Error(ErrorCode.INSUFFICIENT_SPACE);
                    PostEvent(OperationPhase.Failed, ModFileState.FileOperationFailed, error);
                    return error;
                }

                Mod.File.FileStateProgress = 0;
                Mod.File.DownloadingBytesPerSecond = 0;
                Mod.InvokeModUpdated(ModChangeType.DownloadProgress);
                
                PostEvent(OperationPhase.Started, progressFileState);

                if (IsUpdateJob)
                {
                    Error deleteOldError = await ModioClient.DataStorage.DeleteInstalledMod(
                        Mod,
                        _index.GetEntry(Mod).InstalledModfileId
                    );

                    if (deleteOldError)
                    {
                        PostEvent(OperationPhase.Failed, ModFileState.FileOperationFailed, deleteOldError);
                        return deleteOldError;
                    }
                }

                var (downloadError, stream) = await ModioClient.Api.DownloadFile(
                    Mod.File.Download.BinaryUrl,
                    CancellationToken
                );

                if (downloadError)
                {
                    if (downloadError.Code == ErrorCode.OPERATION_CANCELLED)
                        PostEvent(OperationPhase.Cancelled, ModFileState.None);
                    else
                        PostEvent(OperationPhase.Failed, ModFileState.FileOperationFailed, downloadError);
                    return downloadError;
                }
                
                
                error = await ModioClient.DataStorage.InstallModFromStream(
                    Mod,
                    Mod.File.Id,
                    stream,
                    Mod.File.Md5Hash,
                    CancellationToken
                );

                if (error)
                {
                    if(error.Code == ErrorCode.OPERATION_CANCELLED)
                        PostEvent(OperationPhase.Cancelled, ModFileState.None);
                    else
                        PostEvent(OperationPhase.Failed, ModFileState.FileOperationFailed, error);
                    return error;
                }
                
                Mod.File.InstallLocation = ModioClient.DataStorage.GetInstallPath(Mod.Id, Mod.File.Id);

                if (_index == null) // From Shutdown
                {
                    PostEvent(OperationPhase.Cancelled, ModFileState.None);
                    return new Error(ErrorCode.SHUTTING_DOWN);
                }
                
                _index.GetEntry(Mod).FileState = ModFileState.Installed;
                _index.GetEntry(Mod).InstalledModfileId = Mod.File.Id;
                await SaveIndex();

                PostEvent(OperationPhase.Completed, ModFileState.Installed);
                return Error.None;
            }

            internal override void GetPendingSpaceChange(ref long spaceRequired, ref long tempSpaceRequired)
            {
                switch (Phase)
                {
                    case OperationPhase.Checking:
                        spaceRequired += Mod.File.FileSize;
                        if (IsUpdateJob) spaceRequired -= _index.GetEntry(Mod).InstallationSize;
                        
                        break;
                    case OperationPhase.Started:
                    {
                        spaceRequired += (long)(Mod.File.FileSize * (1.0 - Mod.File.FileStateProgress));

                        if (IsUpdateJob && Mod.File.FileStateProgress == 0)
                            spaceRequired -= _index.GetEntry(Mod).InstallationSize;
                        
                        break;
                    }
                }
            }
        }

        class UninstallJob : Job
        {
            public UninstallJob(Mod mod) : base(mod, OperationType.Uninstall) { }

            public override async Task<Error> Run()
            {
                PostEvent(OperationPhase.Started, ModFileState.Uninstalling);

                var indexEntry = _index.GetEntry(Mod);
                var error = await ModioClient.DataStorage.DeleteInstalledMod(Mod, indexEntry.InstalledModfileId);

                _index.RemoveEntry(Mod);
                await SaveIndex();
                PostEvent(OperationPhase.Completed, ModFileState.None);
                _unverifiedMods.Remove(Mod);

                if (_modsToUninstall.Contains(Mod)) _modsToUninstall.Remove(Mod);

                RetryInstallingTaintedMods();

                return error;
            }

            internal override void GetPendingSpaceChange(ref long spaceRequired, ref long tempSpaceRequired)
            {
                spaceRequired -= Mod.File.FileSize;
            }
        }
        
        class ValidateJob : Job
        {
            public ValidateJob(Mod mod) : base(mod, OperationType.Validate)
            {
            }

            public override async Task<Error> Run()
            {
                PostEvent(OperationPhase.Started, Mod.File.State);
                
                var indexEntry = _index.GetEntry(Mod);
                bool doesExist = ModioClient.DataStorage.DoesInstallExist(Mod.Id, indexEntry.InstalledModfileId);

                if (!doesExist)
                {
                    ModioLog.Warning?.Log($"Mod {Mod}: Failed to validate installed mod directory. It will be re-downloaded");
                    
                    PostEvent(OperationPhase.Completed, ModFileState.None);

                    if(!Mod.IsSubscribed)
                        _index.RemoveEntry(Mod);
                    else
                    {
                        indexEntry.FileState = ModFileState.None;
                        indexEntry.InstalledModfileId = 0;
                        indexEntry.InstallationSize = 0;
                    }
                    await SaveIndex();
                    _unverifiedMods.Remove(Mod);
                    return Error.None;
                }
                
                _unverifiedMods.Remove(Mod);
                
                PostEvent(OperationPhase.Completed, ModFileState.Installed);
                return Error.None;
            }

            internal override void GetPendingSpaceChange(ref long spaceRequired, ref long tempSpaceRequired)
            { }
        }

        /// <summary>
        /// A job that scans the "mods" folder for anything that isn't tracked by the index
        /// After this completes and the queue is empty we'll EnqueueJobs again, uninstalling old mods if necessary
        /// </summary>
        class ScanMissingInstallsJob : Job
        {
            public ScanMissingInstallsJob() : base(null, OperationType.Scan)
            {
            }

            public override async Task<Error> Run()
            {
                Phase = OperationPhase.Started;

                bool foundAnyNewMods = await _index.UpdateIndexWithMissingEntriesFromScan();

                if (foundAnyNewMods)
                    await SaveIndex();
                
                _hasScannedMissingMods = true;
                
                Phase = OperationPhase.Completed;
                
                return Error.None;
            }

            internal override void GetPendingSpaceChange(ref long spaceRequired, ref long tempSpaceRequired)
            { }
        }

        public enum OperationType
        {
            Download,
            Install,
            Update,
            Uninstall,
            Validate,
            Scan,
        }

        public enum OperationPhase
        {
            Checking,
            Started,
            Completed,
            Cancelled,
            Failed,
        }

#endregion

        /// <summary>
        /// Returns if there is enough available space to install a Mod, will account for pending jobs.
        /// </summary>
        /// <param name="mod">The mod check available space for</param>
        /// <returns>
        /// An asynchronous task that returns <c>true</c> if there is enough space, <c>false</c> otherwise.
        /// </returns>
        public static async Task<bool> IsThereAvailableSpaceFor(Mod mod)
        {
            long spaceRequired = 0;
            long tempSpaceRequired = 0;

            _currentOperation?.GetPendingSpaceChange(ref spaceRequired, ref tempSpaceRequired);

            foreach (Job job in _operationQueue) job.GetPendingSpaceChange(ref spaceRequired, ref tempSpaceRequired);
            
            if (DownloadAndExtractAsSingleJob)
                return await ModioClient.DataStorage.IsThereAvailableFreeSpaceFor(
                    tempSpaceRequired,
                    spaceRequired + mod.File.FileSize
                );

            return await ModioClient.DataStorage.IsThereAvailableFreeSpaceFor(
                tempSpaceRequired + mod.File.ArchiveFileSize,
                spaceRequired + mod.File.FileSize
            );
        }
        
        /// <summary>
        /// Returns if there is enough available space to install a Mod, will account for pending jobs.
        /// </summary>
        /// <param name="mod">The mod check available space for</param>
        /// <returns>
        /// An asynchronous task that returns <c>true</c> if there is enough space, <c>false</c> otherwise.
        /// </returns>
        public static async Task<bool> IsThereAvailableSpaceFor(ModCollection mod)
        {
            long spaceRequired = 0;
            long tempSpaceRequired = 0;

            _currentOperation?.GetPendingSpaceChange(ref spaceRequired, ref tempSpaceRequired);

            foreach (Job job in _operationQueue) job.GetPendingSpaceChange(ref spaceRequired, ref tempSpaceRequired);
            
            if (DownloadAndExtractAsSingleJob)
                return await ModioClient.DataStorage.IsThereAvailableFreeSpaceFor(
                    tempSpaceRequired,
                    spaceRequired + mod.Filesize
                );

            return await ModioClient.DataStorage.IsThereAvailableFreeSpaceFor(
                tempSpaceRequired + mod.ArchiveFilesize,
                spaceRequired + mod.Filesize
            );
        }

        internal static string GetDebugString()
        {
            string debugString = $"Current operation: {_currentOperation}";
            foreach (Job job in _operationQueue)
            {
                debugString += $"\n{job}";
            }
            return debugString;
        }
    }
}
