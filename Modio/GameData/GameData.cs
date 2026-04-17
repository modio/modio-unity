using System;
using System.Threading.Tasks;
using Modio.API;
using Modio.API.SchemaDefinitions;
using Modio.Extensions;

namespace Modio.Mods
{
    public class GameData
    {   

        public GameTagCategory[] Categories;
        public string CurrencyName;
        public GameCommunityOptions CommunityOptions;
        public GameMonetizationOptions MonetizationOptions;

        bool _hasFetchedWeb;

        static GameData _cachedGameData;
        static Task<(Error error, GameData gameData)> _readTask;
        static Task<(Error error, GameData gameData)> _fetchTask;
        static TaskCompletionSource<Error> _writeTcs;
        static bool _isDirty;

        static GameData()
        {
            //handle restarting the plugin and potentially changing game
            ModioClient.OnShutdown += () => _cachedGameData = null;
        }
        
        public static async Task<(Error error, GameData gameData)> GetGameDataFromDisk()
        {
            if (_cachedGameData is not null)
                return (Error.None, _cachedGameData);
            
            _readTask ??= ModioClient.DataStorage.ReadGameData();
            (Error error, GameData gameData) = await _readTask;

            _cachedGameData = gameData;
            _readTask = null;
            
            return (error, gameData);
        }

        public static async Task<(Error error, GameData data)> GetGameData()
        {
            _fetchTask ??= GetGameDataInternal();

            (Error error, GameData gameData) fetchTaskResult = await _fetchTask;

            _fetchTask = null;

            return fetchTaskResult;
        }

        static async Task<(Error error, GameData data)> GetGameDataInternal()
        {
            (Error diskError, GameData gameData) = await GetGameDataFromDisk();

            if (diskError)
                gameData = new GameData();
            else if (gameData._hasFetchedWeb)
                return (Error.None, gameData);
            
            (Error webError, GameObject? gameObject) = await ModioAPI.Games.GetGame();

            if (webError)
                return (webError, gameData);
            
            gameData.CurrencyName = gameObject.Value.TokenName;
            gameData.CommunityOptions = (GameCommunityOptions)gameObject.Value.CommunityOptions;
            gameData.MonetizationOptions = (GameMonetizationOptions)gameObject.Value.MonetizationOptions;
            
            gameData._hasFetchedWeb = true;

            SetGameData(gameData).ForgetTaskSafely();
            
            return (Error.None, gameData);
        }

        /// <summary>
        /// Sets the game data, writing it to disk. If a write is already in progress, it will mark the data as dirty and wait for the current write to finish before writing again if necessary.
        /// This ensures that we don't have multiple concurrent writes to disk, which could cause issues.
        /// </summary>
        /// <param name="data"> The game data to set. This will be cached in memory and written to disk.</param>
        /// <returns> An error if the write failed, or Error.None if it succeeded.</returns>
        public static async Task<Error> SetGameData(GameData data)
        {
            _cachedGameData = data;
            
            if (_writeTcs is not null)
            {
                _isDirty = true;
                return await _writeTcs.Task;
            }
            
            _isDirty = false;
            _writeTcs = new TaskCompletionSource<Error>();
            Error error;

            do
            {
                _isDirty = false;
                error = await ModioClient.DataStorage.WriteGameData(_cachedGameData);
            }
            while (_isDirty && !error);

            _writeTcs.SetResult(error);
            _writeTcs = null;

            return error;
        }

        /// <summary>
        /// Store the current game data to disk.
        /// </summary>
        /// <returns> An error if the write failed, or Error.None if it succeeded.</returns>
        /// <remarks> This will write the cached game data to disk, and if a write is already in progress, it will wait for it to finish before writing again if necessary.</remarks>
        internal static async Task<Error> StoreGameDataToDisk() => await SetGameData(_cachedGameData);

        public static async Task<Error> SetGameTags(GameTagCategory[] tags)
        {
            (Error error, GameData gameData) = await GetGameDataFromDisk();

            if (error)
            {
                if (_cachedGameData != null)
                    _cachedGameData.Categories = tags;

                return error;
            }
            
            gameData.Categories = tags;
            return await SetGameData(gameData);
        }
    }
}
