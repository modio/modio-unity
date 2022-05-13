using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ModIO.Implementation.API;
using ModIO.Implementation.API.Objects;
using ModIO.Implementation.API.Requests;
using UnityEngine;
using UnityEngine.Networking;

namespace ModIO.Implementation
{
    /// <summary>
    /// This Handles all of the Download/Extract/Install/Delete operations for mods if
    /// isModManagementEnabled is set to true. You can use 'WakeUp()' to alert this class and it
    /// will begin looking for jobs to perform. When it can't find anymore jobs to do it will go
    /// back to sleep until WakeUp() is called again.
    /// </summary>
    internal static class ModManagement
    {
        /// <summary>
        /// This is a cache of the current async method for performing a job. This is primarily
        /// used by the Shutdown method to interrupt and cancel any ongoing tasks/jobs
        /// </summary>
        static Task operation;

        /// <summary>
        /// This is set to true/false via the Enable/DisableModManagement methods and determines
        /// if mod management will look for jobs when WakeUp() is called
        /// </summary>
        static bool isModManagementEnabled;

        /// <summary>
        /// This is true while mod management is performing/looking for a task/job to perform
        /// </summary>
        static bool isModManagementAwake;

        /// <summary>
        /// ModIds that fail to be downloaded/installed/deleted get added to this group and will be
        /// ignored for further operations until ModManagement is turned off and on again.
        /// </summary>
        static HashSet<ModId> taintedMods = new HashSet<ModId>();

        /// <summary>
        /// This list is used to tag orphan mod collection entries that no longer need to exist in
        /// the Registry. Once tagged, they will periodically get cleared when a new ModManagement
        /// job is checked for.
        /// </summary>
        static List<ModId> uninstalledModsWithNoUserSubscriptions = new List<ModId>();

        /// <summary>
        /// This is a cache of creation tokens that have been requested by the user and haven't yet
        /// been used to generate a new mod profile page.
        /// Once a user has used a creation token it is removed from this group and a new token will
        /// need to be generated in order to create another mod profile page.
        /// </summary>
        static HashSet<CreationToken> creationTokens = new HashSet<CreationToken>();

        /// <summary>
        /// This is a cached reference of the current job being performed.
        /// </summary>
        public static ModManagementJob currentJob;

        /// <summary>
        /// We keep a reference to all of the previous jobs that we've completed to make sure we
        /// aren't repeating the same job with the same modId twice (If it succeeded). This is a
        /// safety measure in case an operation fails or doesn't do what it was meant to but returns
        /// a succeeded result regardless.
        /// </summary>
        static Dictionary<long, ModManagementOperationType> previousJobs =
            new Dictionary<long, ModManagementOperationType>();

        /// <summary>
        /// Delegate that gets invoked whenever mod management starts, fails or ends a task/job
        /// </summary>
        public static ModManagementEventDelegate modManagementEventDelegate;

#region Creation Tokens
        public static CreationToken GenerateNewCreationToken()
        {
            CreationToken token = new CreationToken();
            creationTokens.Add(token);
            return token;
        }

        public static void InvalidateCreationToken(CreationToken token)
        {
            if(creationTokens.Contains(token))
            {
                creationTokens.Remove(token);
            }
        }

        public static bool IsCreationTokenValid(CreationToken token)
        {
            if(token == null)
            {
                return false;
            }
            return creationTokens.Contains(token);
        }
#endregion // Creation Tokens

        public static void EnableModManagement()
        {
            isModManagementEnabled = true;
            WakeUp();
        }

        public static void DisableModManagement()
        {
            isModManagementEnabled = false;
        }

        /// <summary>
        /// Call this whenever a change has been made that may require the ModManagement class to
        /// perform an action (Such as FetchUpdates, Subscribe, Unsubscribe). Automatically checks
        /// queues and tasks. If the ModManagement system is already awake this call will be
        /// ignored silently.
        /// </summary>
        public static async void WakeUp()
        {
            // Early out (dont have more than one instance of this running)
            if(isModManagementAwake || !isModManagementEnabled)
            {
                return;
            }

            // AWAKE
            isModManagementAwake = true;
            
            // Wait until we have no more jobs to run
            operation = PerformJobs();
            await operation;

            // SLEEP
            isModManagementAwake = false;
        }

        static async Task PerformJobs()
        {
            // Look for a job to be performed
            currentJob = await GetNextModManagementJob();

            while(currentJob != null && isModManagementEnabled)
            {
                Result result = await PerformJob(currentJob);

                if(!result.Succeeded())
                {
                    Logger.Log(LogLevel.Error,
                               "ModManagement Failed to complete an operation."
                                   + $" the Mod[{currentJob.mod.modObject.id}_{currentJob.mod.modObject.modfile.id}]"
                                   + " will be ignored by ModManagement for the duration"
                                   + " of the session.");

                    taintedMods.Add((ModId)currentJob.mod.modObject.id);
                }
                
                ModCollectionManager.SaveRegistry();

                currentJob = isModManagementEnabled ? await GetNextModManagementJob() : null;
            }
            currentJob = null;
        }

        public static async Task<Result> PerformJob(ModManagementJob job)
        {
            Result result = ResultBuilder.Unknown;

            // Early out, This should never happen
            if(job == null)
            {
                return ResultBuilder.Create(ResultCode.Internal_ModManagementOperationFailed);
            }

            long modId = job.mod.modObject.id;
            long fileId = job.mod.modObject.modfile.id;
            long currentFileId = job.mod.currentModfile.id;

            // Check for unwanted behaviour, possible infinite loop
            if(previousJobs.ContainsKey(modId))
            {
                if(previousJobs[modId] == job.type)
                {
                    Logger.Log(LogLevel.Error,
                               $"Mod Management [{modId}_{job.type.ToString()}"
                                   + $"_{currentJob.mod.modObject.modfile.id}]"
                                   + $" has received an identical job that should "
                                   + $"already be complete. To avoid getting into an "
                                   + $"infinite loop the ModId[{modId}] has been "
                                   + $"blacklisted and will be ignored until "
                                   + $"ModManagement is re-enabled.");
                    return ResultBuilder.Create(ResultCode.Internal_ModManagementOperationFailed);
                }
            }

            //-------------------[ SETUP PROGRESS HANDLE ]-----------------//
            job.progressHandle = new ProgressHandle();

            switch(job.type)
            {
                case ModManagementOperationType.Install:
                    //--------------------------------------------------------------------------------//
                    //                              INSTALL / EXTRACT //
                    //--------------------------------------------------------------------------------//

                    job.progressHandle.OperationType = ModManagementOperationType.Install;

                    InvokeModManagementDelegate((ModId)modId,
                                                ModManagementEventType.InstallStarted);

                    result = await PerformOperation_Install(job);

                    job.progressHandle.Completed = true;

                    if(result.Succeeded())
                    {
                        InvokeModManagementDelegate((ModId)modId, ModManagementEventType.Installed);
                    }
                    else
                    {
                        job.progressHandle.Failed = true;
                        InvokeModManagementDelegate((ModId)modId,
                                                    ModManagementEventType.InstallFailed);
                    }

                    break;
                case ModManagementOperationType.Download:
                    //--------------------------------------------------------------------------------//
                    //                                   DOWNLOAD //
                    //--------------------------------------------------------------------------------//

                    job.progressHandle.OperationType = ModManagementOperationType.Download;

                    InvokeModManagementDelegate((ModId)modId,
                                                ModManagementEventType.DownloadStarted);

                    result = await PerformOperation_Download(job);

                    job.progressHandle.Completed = true;

                    // Check download result
                    if(result.Succeeded())
                    {
                        InvokeModManagementDelegate((ModId)modId,
                                                    ModManagementEventType.Downloaded);
                    }
                    else
                    {
                        job.progressHandle.Failed = true;
                        InvokeModManagementDelegate((ModId)modId,
                                                    ModManagementEventType.DownloadFailed);
                    }

                    break;
                case ModManagementOperationType.Update:
                    //----------------------------------------------------------------------------//
                    //                                   UPDATE                                   //
                    //----------------------------------------------------------------------------//

                    job.progressHandle.OperationType = ModManagementOperationType.Update;

                    // TODO @Steve everything in this block is repeated code. Create re-useable
                    // methods for download, install, delete

                    // Make sure we aren't trying to update something that is already up to date
                    if(fileId == currentFileId)
                    {
                        Logger.Log(LogLevel.Error,
                                   $"Mod Management Was given a job to "
                                       + $"update a mod but the existing fileId matches"
                                       + $" the update fileId, therefore should already"
                                       + $" be up to date [{modId}_{currentFileId} == "
                                       + $"{modId}_{fileId}].");
                        result =
                            ResultBuilder.Create(ResultCode.Internal_ModManagementOperationFailed);
                        break;
                    }

                    //---------------------------[ DOWNLOAD UPDATE ]------------------------------//

                    InvokeModManagementDelegate((ModId)modId, ModManagementEventType.UpdateStarted);

                    result = await PerformOperation_Download(job);

                    if(!result.Succeeded())
                    {
                        job.progressHandle.Completed = true;
                        job.progressHandle.Failed = true;
                        InvokeModManagementDelegate((ModId)modId,
                                                    ModManagementEventType.UpdateFailed);
                        break;
                    }

                    //---------------------------[ INSTALL UPDATE ]-------------------------------//

                    result = await PerformOperation_Install(job);

                    job.progressHandle.Completed = true;

                    if(result.Succeeded())
                    {
                        InvokeModManagementDelegate((ModId)modId, ModManagementEventType.Updated);
                    }
                    else
                    {
                        job.progressHandle.Failed = true;
                        InvokeModManagementDelegate((ModId)modId,
                                                    ModManagementEventType.UpdateFailed);
                    }

                    break;
                case ModManagementOperationType.Uninstall:
                    //----------------------------------------------------------------------------//
                    //                                  UNINSTALL                                 //
                    //----------------------------------------------------------------------------//

                    job.progressHandle.OperationType = ModManagementOperationType.Uninstall;

                    result = await PerformOperation_Delete(modId, job.mod.currentModfile.id);

                    // We dont really track this process as it's nearly instantaneous
                    job.progressHandle.Progress = 1f;
                    job.progressHandle.Completed = true;

                    if(result.Succeeded())
                    {
                        InvokeModManagementDelegate((ModId)modId,
                                                    ModManagementEventType.Uninstalled);
                    }
                    else
                    {
                        job.progressHandle.Failed = true;
                        InvokeModManagementDelegate((ModId)modId,
                                                    ModManagementEventType.UninstallFailed);
                    }

                    break;
            }

            if(previousJobs.ContainsKey(modId))
            {
                previousJobs[modId] = job.type;
            }
            else
            {
                previousJobs.Add(modId, job.type);
            }
            return result;
        }

        // The following PerformOperation methods are all used by the PerformJob method.
        // PerformJob will determine the type of job and use the following methods accordingly.
        static async Task<Result> PerformOperation_Download(ModManagementJob job)
        {
            long modId = job.mod.modObject.id;
            long fileId = job.mod.modObject.modfile.id;

            Logger.Log(LogLevel.Message, $"DOWNLOADING MODFILE[{modId}_{fileId}]");

            Result result = ResultBuilder.Unknown;

            // Get Mod
            string url = API.Requests.GetMod.URL(modId);
            ResultAnd<ModObject> modResponse =
                await RESTAPI.Request<ModObject>(url, GetMod.Template);
            if(!modResponse.result.Succeeded())
            {
                // If this fails we continue by trying out the fileId we have already
                Logger.Log(LogLevel.Warning,
                           $"Failed to get mod[{modId}] for modfile download. "
                               + $"Attempting to use existing download url in cache instead.");
            }
            else
            {
                job.mod.modObject = modResponse.value;
                
                //Re-cache file id in case of an update/patch to the mod
                fileId = job.mod.modObject.modfile.id;
                
                // Update the registry for this mod as it's now the newest instance of this mod we have
                ModCollectionManager.UpdateModCollectionEntry((ModId)modResponse.value.id, modResponse.value);
            }

            if(!ShouldModManagementBeRunning())
            {
                goto Cleanup;
            }

            // Get Modfile url from ModObject
            string fileURL = job.mod.modObject.modfile.download.binary_url;

            // Get correctMD5 and download location
            string md5 = job.mod.modObject.modfile.filehash.md5;
            string downloadFilepath = DataStorage.GenerateModfileArchiveFilePath(modId, fileId);

            // Begin download
            ResultAnd<string> downloadResponse = await RESTAPI.Request<string>(
                fileURL, DownloadBinary.Template, null, new DownloadHandlerFile(downloadFilepath),
                job.progressHandle);

            // Check download result
            if(downloadResponse.result.Succeeded())
            {
                // Always check after an async await
                if(!ShouldModManagementBeRunning())
                {
                    result = ResultBuilder.Create(ResultCode.Internal_OperationCancelled);
                }
                else
                {
                    result = ResultBuilder.Success;
                    Logger.Log(LogLevel.Verbose, $"DOWNLOADED MODFILE[{modId}_{fileId}]");
                }
            }
            else
            {
                Logger.Log(LogLevel.Error, $"Failed to download modfile[{modId}_{fileId}]");

                result = ResultBuilder.Create(ResultCode.Internal_ModManagementOperationFailed);
                goto Cleanup;
            }

            // Make sure the downloaded file MD5 matches the given MD5
            if(await ValidateDownload_md5(md5, downloadFilepath))
            {
                if(!ShouldModManagementBeRunning())
                {
                    result = ResultBuilder.Create(ResultCode.Internal_OperationCancelled);
                }
                else
                {
                    Logger.Log(LogLevel.Verbose, $"VERIFIED DOWNLOAD[{modId}_{fileId}]");
                }
            }
            else
            {
                Logger.Log(LogLevel.Error,
                           $"Failed to verify downloaded modfile[{modId}_{fileId}]");
                result = ResultBuilder.Create(ResultCode.Internal_ModManagementOperationFailed);
                goto Cleanup;
            }

            return result;

        // If any of the blocks above fail we jump to the cleanup and end of method
        Cleanup:;

            if(!result.Succeeded())
            {
                // cleanup any file that may or may not have downloaded because it's corrupted
                DataStorage.TryDeleteModfileArchive(
                    modId, fileId, out Result notBeingCheckedBecauseThisIsACleanup);
            }

            return result;
        }

        static async Task<Result> PerformOperation_Install(ModManagementJob job)
        {
            long modId = job.mod.modObject.id;
            long fileId = job.mod.modObject.modfile.id;

            Logger.Log(LogLevel.Verbose, $"INSTALLING MODFILE[{modId}_{fileId}]");

            ExtractOperation extractOperation =
                new ExtractOperation(modId, fileId, job.progressHandle);

            // Cached so it can be cancelled on shutdown
            job.zipOperation = extractOperation;

            Result result = await extractOperation.Extract();

            // Always check after an async await
            if(!ShouldModManagementBeRunning())
            {
                result = ResultBuilder.Create(ResultCode.Internal_OperationCancelled);
            }

            if(result.Succeeded())
            {
                // try to cleanup any existing outdated modfile
                if(fileId != job.mod.currentModfile.id)
                {
                    long currentFileId = job.mod.currentModfile.id;
                    if(DataStorage.TryGetInstallationDirectory(modId, currentFileId,
                                                               out string notusingthishere))
                    {
                        DataStorage.TryDeleteInstalledMod(modId, currentFileId, out result);
                        if(!result.Succeeded())
                        {
                            // This is only a warning because we dont want to fail the installation
                            // because of a leftover/unused modfile not getting properly deleted
                            Logger.Log(LogLevel.Warning,
                                       $"Failed to cleanup old modfile[{modId}_{currentFileId}]");
                        }
                    }
                }

                // Set currentModfile to the existing modfile (because we succeeded to install)
                job.mod.currentModfile = job.mod.modObject.modfile;
                ModCollectionManager.UpdateModCollectionEntryFromModObject(job.mod.modObject);
                Logger.Log(LogLevel.Verbose, $"INSTALLED MOD [{modId}_{fileId}]");
            }
            else
            {
                Logger.Log(LogLevel.Error, $"Failed to install mod[{modId}_{fileId}]");
                result = ResultBuilder.Create(ResultCode.Internal_ModManagementOperationFailed);
            }

            return result;
        }

        static async Task<Result> PerformOperation_Delete(long modId, long fileId)
        {
            Logger.Log(LogLevel.Verbose, $"DELETING MODFILE[{modId}_{fileId}]");
            DataStorage.TryDeleteInstalledMod(modId, fileId, out Result result);
            if(!result.Succeeded())
            {
                Logger.Log(LogLevel.Error, $"Failed to delete modfile[{modId}_{fileId}]");
            }
            else
            {
                Logger.Log(LogLevel.Verbose, $"DELETED MODFILE[{modId}_{fileId}");
            }

            return await Task.FromResult(result);
        }

        public static async Task ShutdownOperations()
        {
            DisableModManagement();
            // Check if a current job is running
            if(currentJob != null)
            {
                // shutdown zip operation if one exists
                if(currentJob.zipOperation != null)
                {
                    currentJob.zipOperation.Cancel();
                    if(currentJob.zipOperation.Operation != null)
                    {
                        await currentJob.zipOperation.Operation;
                    }
                }
                // shutdown download request if one exists
                if(currentJob?.downloadWebRequest != null)
                {
                    // Don't Dispose(), this will happen in RESTAPI.cs
                    currentJob.downloadWebRequest.Abort();
                }
            }

            if(operation != null)
            {
                await operation;
            }

            creationTokens.Clear();
            previousJobs.Clear();
            taintedMods.Clear();
        }

        /// <summary>
        /// returns null if there isn't any active current job
        /// </summary>
        /// <returns></returns>
        public static ProgressHandle GetCurrentOperationProgress()
        {
            if(currentJob == null || currentJob.progressHandle == null)
            {
                return null;
            }

            currentJob.progressHandle.modId = (ModId)currentJob.mod.modObject.id;
            
            return currentJob.progressHandle;
        }

        public static void InvokeModManagementDelegate(ModId modId,
                                                       ModManagementEventType eventType)
        {
            modManagementEventDelegate?.Invoke(eventType, modId);
        }

        public static SubscribedModStatus GetModCollectionEntrysSubscribedModStatus(
            ModCollectionEntry mod)
        {
            ModId modId = (ModId)mod.modObject.id;
            long fileId = mod.modObject.modfile.id;
            long currentFileId = mod.currentModfile.id;
            
            // This block is nearly identical to GetNextJobTypeForModCollectionEntry()
            // except without the async md5 check
            if(taintedMods.Contains(modId))
            {
                return SubscribedModStatus.ProblemOccurred;
            } 
            if(ShouldThisModBeUninstalled(modId))
            {
                if(DataStorage.TryGetInstallationDirectory(modId, currentFileId,
                    out string notbeingusedhere))
                {
                    return SubscribedModStatus.WaitingToUninstall;
                }
            }
            else if(DataStorage.TryGetInstallationDirectory(modId, currentFileId,
                out string notbeingusedhere))
            {
                if(currentFileId != fileId)
                {
                    return SubscribedModStatus.WaitingToUpdate;
                }
            }
            else if(DataStorage.TryGetModfileArchive(modId, fileId, out string downloadFilepath))
            {
                return SubscribedModStatus.WaitingToInstall;
            }
            else
            {
                return SubscribedModStatus.WaitingToDownload;
            }

            return SubscribedModStatus.Installed;
        }

        static async Task<ModManagementJob> GetNextModManagementJob()
        {
            // Early out
            if(!ShouldModManagementBeRunning())
            {
                return null;
            }

            // TODO @Steve add a function to add high priority to specific operations/modIds

            // enumerate over UserData subscribedMods
            // check if mod is still subscribed
            ModManagementJob job = null;
            uninstalledModsWithNoUserSubscriptions.Clear();

            // TODO I'm going to try and benchmark this later to see if it causes blocking issues
            // (what are the limits in mod count?)
            using(var enumerator = ModCollectionManager.Registry.mods.GetEnumerator())
            {
                while(enumerator.MoveNext())
                {
                    // Check if we should still be running
                    if(!ShouldModManagementBeRunning())
                    {
                        return null;
                    }

                    ModManagementOperationType jobType =
                        await GetNextJobTypeForModCollectionEntry(enumerator.Current.Value);

                    if(jobType != ModManagementOperationType.None_AlreadyInstalled
                       && jobType != ModManagementOperationType.None_ErrorOcurred)
                    {
                        // Valid job found
                        job =
                            new ModManagementJob { mod = enumerator.Current.Value, type = jobType };
                        break;
                    }
                }
            }

            // Remove any collections that no longer have subscribed users and installed files
            foreach(ModId mod in uninstalledModsWithNoUserSubscriptions)
            {
                ModCollectionManager.Registry.mods.Remove(mod);
            }

            return job;
        }

        static async Task<ModManagementOperationType> GetNextJobTypeForModCollectionEntry(
            ModCollectionEntry mod)
        {
            ModId modId = (ModId)mod.modObject.id;
            long fileId = mod.modObject.modfile.id;
            long currentFileId = mod.currentModfile.id;

            if(taintedMods.Contains(modId))
            {
                // This mod has been marked as having an error, we will ignore it for the session
                return ModManagementOperationType.None_ErrorOcurred;
            }

            bool delete = ShouldThisModBeUninstalled(modId);

            if(delete)
            {
                if(DataStorage.TryGetInstallationDirectory(modId, currentFileId,
                                                           out string notbeingusedhere))
                {
                    return ModManagementOperationType.Uninstall;
                }

                // NOT INSTALLED (Tag entry for cleanup)
                uninstalledModsWithNoUserSubscriptions.Add(modId);
            }
            else if(DataStorage.TryGetInstallationDirectory(modId, currentFileId,
                                                            out string notbeingusedhere))
            {
                // INSTALLED (Check for update)
                if(currentFileId != fileId)
                {
                    return ModManagementOperationType.Update;
                }
            }
            else if(DataStorage.TryGetModfileArchive(modId, fileId, out string downloadFilepath))
            {
                // Make sure the installed modfile has the correct md5
                if (!await ValidateDownload_md5(mod.modObject.modfile.filehash.md5, downloadFilepath))
                {
                    // if the md5 is incorrect re-download the file
                    // (it may have been interrupted from a previous session)
                    // TODO NOTE: @Steve look into what needs to be done for resuming downloads
                    return ModManagementOperationType.Download;
                }
                
                return ModManagementOperationType.Install;
            }
            else
            {
                return ModManagementOperationType.Download;
            }

            return ModManagementOperationType.None_AlreadyInstalled;
        }

        static bool ShouldThisModBeUninstalled(ModId modId)
        {
            List<long> users = new List<long>();

            using(var enumerator = ModCollectionManager.Registry.existingUsers.GetEnumerator())
            {
                // Get all users that are subscribed to this mod
                while(enumerator.MoveNext())
                {
                    if(enumerator.Current.Value.subscribedMods.Contains(modId))
                    {
                        users.Add(enumerator.Current.Key);
                    }
                }
            }

            if(users.Count == 0)
            {
                // No subscribed users, we can uninstall this mod
                return true;
            }

            if(ModCollectionManager.Registry.mods.TryGetValue(modId, out ModCollectionEntry mod))
            {
                if(users.Contains(ModCollectionManager.GetUserKey()))
                {
                    // Current user is subscribed, don't uninstall
                    return false;
                }
                if(mod.uninstallIfNotSubscribedToCurrentSession)
                {
                    // This mod has been marked to uninstall if not subscribed to the current user
                    return true;
                }
            }
            else
            {
                Logger.Log(LogLevel.Warning,
                           "A ModManagement check was made on a "
                               + "ModCollectionEntry that doesn't appear to exist "
                               + "anymore in the registry. This shouldn't happen.");
                return false;
            }

            return false;
        }

        public static async Task<bool> ValidateDownload_md5(string correctMD5,
                                                            string zippedFilepath)
        {
            string md5 = await IOUtil.GenerateMD5Async(zippedFilepath);

            bool isCorrect = correctMD5.Equals(md5);
            if(!isCorrect)
            {
                Logger.Log(LogLevel.Warning, "Failed to validate modfile archive with md5."
                                             + " This may be due to a download being corrupted or "
                                             + "stopped before completion, such as from "
                                             + "ModIOUnity.Shutdown()");
            }
            return isCorrect;
        }

        /// <summary>
        /// Checks if ModManagement should be running, whether it's still enabled, initialized and
        /// credentials are valid. (Automatically disables ModManagement if not initialized)
        /// </summary>
        /// <returns></returns>
        static bool ShouldModManagementBeRunning()
        {
            // Early out
            if(!isModManagementEnabled
               || !ModIOUnityImplementation.AreCredentialsValid(true, out Result credentialsResult))
            {
                return false;
            }
            if(!ModIOUnityImplementation.IsInitialized(out Result initResult))
            {
                // we aren't initialize
                DisableModManagement();
                return false;
            }

            return true;
        }
    }
}
