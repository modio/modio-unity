using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Modio.Mods;
using Modio.Users;

namespace Modio.FileIO
{
    /// <summary>Interface for the platform file IO services</summary>
    public interface IModioDataStorage
    {
        Task<Error> Init();
        
        /// <summary>Forces the file IO service to immediately stop all operations and shutdown.</summary>
        /// <returns>An asynchronous task</returns>
        Task Shutdown();

        /// <summary>Deletes all saved game data.</summary>
        /// <returns>
        /// An asynchronous task that returns <see cref="Error"/>.<see cref="Error.None"/> on success.
        /// </returns>
        Task<Error> DeleteAllGameData();

#region Game Data

        /// <summary>Reads the game_data.json file for the initialized Game ID and deserializes it into a GameTagsObject.</summary>
        /// <returns>
        /// <p>An asynchronous task that returns a tuple (<see cref="Error"/> error, <see cref="GameData"/> result), where:</p>
        /// <p><c>error</c> is the error encountered during the task (if any)</p>
        /// <p><c>result</c> is the game data.</p>
        /// </returns>
        Task<(Error error, GameData result)> ReadGameData();

        /// <summary>Writes the given GameData into JSON data and writes it to the game_data.json file for the initialized Game ID.</summary>
        /// <param name="gameData">The GameData to be written.</param>
        /// <returns>
        /// An asynchronous task that returns <see cref="Error"/>.<see cref="Error.None"/> on success.
        /// </returns>
        Task<Error> WriteGameData(GameData gameData);

        /// <summary>Deletes the game_data.json file for the initialized Game ID.</summary>
        /// <returns>
        /// An asynchronous task that returns <see cref="Error"/>.<see cref="Error.None"/> on success.
        /// </returns>
        Task<Error> DeleteGameData();

#endregion

#region Mod Index

        /// <summary>
        /// Reads the <c>[GameId]_mod_index.json</c> file for the initialized Game ID and deserializes it
        /// into a <see cref="ModIndex"/> object.
        /// </summary>
        /// <returns>
        /// <p>An asynchronous task that returns a tuple (<see cref="Error"/> error, <see cref="ModIndex"/> index), where:</p>
        /// <p><c>error</c> is the error encountered during the task (if any)</p>
        /// <p><c>index</c> is the index data.</p>
        /// </returns>
        Task<(Error error, ModIndex index)> ReadIndexData();

        /// <summary>
        /// Serializes the given <see cref="ModIndex"/> and saves it to the initialized Game ID's
        /// <c>[GameId]_mod_index.json</c> file.
        /// </summary>
        /// <param name="index">The index data to be written</param>
        /// <returns>
        /// An asynchronous task that returns <see cref="Error"/>.<see cref="Error.None"/> on success.
        /// </returns>
        Task<Error> WriteIndexData(ModIndex index);

        /// <summary>Deletes the <c>[GameId]_mod_index.json</c> file for the initialized Game ID.</summary>
        /// <returns>
        /// An asynchronous task that returns <see cref="Error"/>.<see cref="Error.None"/> on success.
        /// </returns>
        Task<Error> DeleteIndexData();

#endregion

#region User Data

        /// <summary>Reads all user_data.json files in the file system and deserializes them into UserObjects.</summary>
        /// <returns>
        /// <p>An asynchronous task that returns a tuple(<see cref="Error"/> error, <see cref="UserSaveObject"/>[] results), where:</p>
        /// <p><c>error</c> is the error encountered during the task (if any)</p>
        /// <p><c>results</c> is the array of user save data.</p>
        /// </returns>
        Task<(Error error, UserSaveObject[] results)> ReadAllSavedUserData();

        /// <summary>Reads the user_data.json file for the specified local user and deserializes it into a UserObject.</summary>
        /// <param name="localUserId">The local userId</param>
        /// <returns>
        /// <p>An asynchronous task that returns a tuple(<see cref="Error"/> error, <see cref="UserSaveObject"/> result), where:</p>
        /// <p><c>error</c> is the error encountered during the task (if any)</p>
        /// <p><c>result</c> is the user save data.</p>
        /// </returns>
        Task<(Error error, UserSaveObject result)> ReadUserData(string localUserId);

        /// <summary>Serializes the given UserObject into JSON data & writes to the user_data.json file for the specified local user.</summary>
        /// <param name="userObject">The UserSaveObject to be written</param>
        /// <returns>
        /// An asynchronous task that returns <see cref="Error"/>.<see cref="Error.None"/> on success.
        /// </returns>
        Task<Error> WriteUserData(UserSaveObject userObject);

        /// <summary>Deletes the user_data.json file for the specified local user.</summary>
        /// <param name="localUserId">The local userId</param>
        /// <returns>
        /// An asynchronous task that returns <see cref="Error"/>.<see cref="Error.None"/> on success.
        /// </returns>
        Task<Error> DeleteUserData(string localUserId);

#endregion

#region Modfile

        /// <summary>Downloads the specified mod from the given download stream.</summary>
        /// <param name="modId">The ID of the mod being downloaded.</param>
        /// <param name="modfileId">The ID of the modfile being downloaded.</param>
        /// <param name="downloadStream">The response stream from the web response.</param>
        /// <param name="md5Hash">The expected md5 hash of the download.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>
        /// An asynchronous task that returns <see cref="Error"/>.<see cref="Error.None"/> on success.
        /// </returns>
        Task<Error> DownloadModFileFromStream(long modId, long modfileId, Stream downloadStream, string md5Hash, CancellationToken token);

        /// <summary>Deletes the Modfile for the specified Mod.</summary>
        /// <param name="modId">The ID of the mod being deleted.</param>
        /// <param name="modfileId">The ID of the modfile being deleted.</param>
        /// <returns>
        /// An asynchronous task that returns <see cref="Error"/>.<see cref="Error.None"/> on success.
        /// </returns>
        Task<Error> DeleteModfile(long modId, long modfileId);

        /// <summary>
        /// Scans for all modfiles by scanning the file system for zip files in /Modfiles that match the convention
        /// of <c>[ModId]-[ModfileId]_modfile.zip</c>.
        /// </summary>
        /// <returns>
        /// <p>An asynchronous task that returns a tuple(<see cref="Error"/> error, List&lt;(long modId, long modfileId)&gt; results), where:</p>
        /// <p><c>error</c> is the error encountered during the task (if any)</p>
        /// <p><c>results</c> is a list of mod Ids and modfile Ids that were found for modfiles.</p>
        /// </returns>
        Task<(Error error, List<(long modId, long modfileId)> results)> ScanForModfiles();

#endregion

#region Installation

        /// <summary>Installs the specified Mod from an already stored Modfile.</summary>
        /// <param name="mod">The mod being installed.</param>
        /// <param name="modfileId">The ID of the modfile being installed.</param>
       /// <param name="token">The cancellation token.</param>
        /// <returns>
        /// An asynchronous task that returns <see cref="Error"/>.<see cref="Error.None"/> on success.
        /// </returns>
        Task<Error> InstallMod(Mod mod, long modfileId, CancellationToken token);

        /// <summary>Installs the specified Mod from any kind of stream (likely file or web).</summary>
        /// <param name="mod">The mod being intalled.</param>
        /// <param name="modfileId">The ID of the modfile being installed.</param>
        /// <param name="stream">The stream being installed from.</param>
        /// <param name="md5Hash">The expected md5 hash of the modfile.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>
        /// An asynchronous task that returns <see cref="Error"/>.<see cref="Error.None"/> on success.
        /// </returns>
        Task<Error> InstallModFromStream(Mod mod, long modfileId, Stream stream, string md5Hash, CancellationToken token);

        /// <summary>Deletes the specified installed Mod.</summary>
        /// <param name="mod">The mod being intalled.</param>
        /// <param name="modfileId">The ID of the modfile being installed.</param>
        /// <returns>
        /// An asynchronous task that returns <see cref="Error"/>.<see cref="Error.None"/> on success.
        /// </returns>
        Task<Error> DeleteInstalledMod(Mod mod, long modfileId);

        /// <summary>
        /// Scans for all mod installations by scanning the file system for directories that match the convention
        /// of <c>[ModId]_[ModfileId]</c>.
        /// </summary>
        /// <returns>
        /// <p>An asynchronous task that returns a tuple(<see cref="Error"/> error, List&lt;(long modId, long modfileId)&gt; results), where:</p>
        /// <p><c>error</c> is the error encountered during the task (if any)</p>
        /// <p><c>results</c> is a list of mod Ids and modfile Ids that were found for installed mods.</p>
        /// </returns>
        Task<(Error error, List<(long modId, long modfileId)> results)> ScanForInstalledMods();

#endregion

#region Images

        /// <summary>Reads an image from the file system at the given path relative to a cache folder.</summary>
        /// <param name="serverPath">The path</param>
        /// <returns>
        /// <p>An asynchronous task that returns a tuple(<see cref="Error"/> error, <see cref="byte"/>[]) results), where:</p>
        /// <p><c>error</c> is the error encountered during the task (if any).</p>
        /// <p><c>result</c> the raw bytes of the image.</p>
        /// </returns>
        Task<(Error error, byte[] result)> ReadCachedImage(Uri serverPath);

        /// <summary>Writes an image encoded in the given byte array to the file system at the given path relative to a cache folder.</summary>
        /// <param name="serverPath">The path</param>
        /// <param name="data">The byte array</param>
        /// <returns>
        /// An asynchronous task that returns <see cref="Error"/>.<see cref="Error.None"/> on success.
        /// </returns>
        Task<Error> WriteCachedImage(Uri serverPath, byte[] data);

        /// <summary>Deletes an image on the file system at the given path relative to a cache folder.</summary>
        /// <param name="serverPath">The path</param>
        /// <returns>
        /// An asynchronous task that returns <see cref="Error"/>.<see cref="Error.None"/> on success.
        /// </returns>
        Task<Error> DeleteCachedImage(Uri serverPath);

#endregion

#region Drive Space

        /// <summary>
        /// Checks if there is enough space for download size and install size.
        /// </summary>
        /// <param name="bytesDownload">The size of the Modfile</param>
        /// <param name="bytesInstall">The size of the (uncompressed) files</param>
        /// <returns>
        /// An asynchronous task that return <c>true</c> if there is available space, <c>false</c> otherwise.
        /// </returns>
        Task<bool> IsThereAvailableFreeSpaceFor(long bytesDownload, long bytesInstall);
        
        /// <summary>Checks if there's enough available free space on the drive to download a Modfile.</summary>
        /// <param name="bytes">The size of the Modfile.</param>
        /// <returns>
        /// An asynchronous task that return a <c>true</c> if there is available space, <c>false</c> otherwise.
        /// </returns>
        Task<bool> IsThereAvailableFreeSpaceForModfile(long bytes);

        /// <summary>Gets the amount of available free space in bytes on the drive for a Modfile.</summary>
        /// <returns>
        /// An asynchronous task that return the amount of available free space in bytes 
        /// </returns>
        Task<long> GetAvailableFreeSpaceForModfile();

        /// <summary>Checks if there's enough available free space on the drive to install a Mod from a Modfile.</summary>
        /// <param name="bytes">The size of the Mod Install.</param>
        /// <returns>
        /// An asynchronous task that return a <c>true</c> if there is available space, <c>false</c> otherwise.
        /// </returns>
        Task<bool> IsThereAvailableFreeSpaceForModInstall(long bytes);

        /// <summary>Gets the amount of available free space in bytes on the drive for a Mod Install.</summary>
        /// <returns>
        /// An asynchronous task that return a the amount of available free space in bytes 
        /// </returns>
        Task <long> GetAvailableFreeSpaceForModInstall();

#endregion

#region File Paths

        /// <summary>Gets the file path for a Modfile for the given Mod ID & Modfile ID.</summary>
        string GetModfilePath(long modId, long modfileId);

        /// <summary>Gets the directory path for a mod installation for the given Mod ID & Modfile ID.</summary>
        string GetInstallPath(long modId, long modfileId);

        /// <summary>Checks if a Modfile is present on the file system.</summary>
        bool DoesModfileExist(long modId, long modfileId);

        /// <summary>Checks if a mod installation is present on the file system.</summary>
        bool DoesInstallExist(long modId, long modfileId);

#endregion
        
#region other

        /// <summary> Compress a file or directory to a ZIP and output the ZIP to a stream</summary>
        /// <param name="filePath">The source path or directory.</param>
        /// <param name="outputTo">The output stream</param>
        /// <returns>
        /// An asynchronous task that returns <see cref="Error"/>.<see cref="Error.None"/> on success.
        /// </returns>
        Task<Error> CompressToZip(string filePath, Stream outputTo);
#endregion
    }
}
