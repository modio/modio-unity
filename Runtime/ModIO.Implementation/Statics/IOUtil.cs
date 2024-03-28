using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

using Texture2D = UnityEngine.Texture2D;
using ImageConversion = UnityEngine.ImageConversion;

namespace ModIO.Implementation
{
    /// <summary>Implements utility functions for working with IO data.</summary>
    internal static class IOUtil
    {
        /// <summary>Attempts to parse the data of a JSON file.</summary>
        public static bool TryParseUTF8JSONData<T>(byte[] data, out T jsonObject, out Result result)
        {
            if(data != null)
            {
                try
                {
                    string dataString = Encoding.UTF8.GetString(data);
                    jsonObject = JsonConvert.DeserializeObject<T>(dataString);
                    result = ResultBuilder.Success;
                    return true;
                }
                catch(Exception e)
                {
                    Logger.Log(LogLevel.Error, $"Failed to deserialize: {e.Message}");
                }
            }

            jsonObject = default(T);
            result = ResultBuilder.Create(ResultCode.Internal_FailedToDeserializeObject);
            return false;
        }

        /// <summary>Generates the byte array for a JSON representation.</summary>
        public static byte[] GenerateUTF8JSONData<T>(T jsonObject)
        {
            byte[] data = null;

            try
            {
                string dataString = JsonConvert.SerializeObject(jsonObject);
                data = Encoding.UTF8.GetBytes(dataString);
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Error, $"Failed to serialize jsonObject. Exception: {e.Message}");
                data = null;
            }

            return data;
        }

        /// <summary>Parse PNG/JPG data as image.</summary>
#if UNITY_2019_4_OR_NEWER
        public static bool TryParseImageData(byte[] data, out Texture2D texture, out Result result)
        {
            if(data == null || data.Length == 0)
            {
                result = ResultBuilder.Create(ResultCode.Internal_InvalidParameter);
                texture = null;

                Logger.Log(LogLevel.Verbose,
                           ":INTERNAL: Attempted to parse image from NULL/0-length buffer.");
            }
            else
            {
                texture = new Texture2D(0, 0);

                bool success = ImageConversion.LoadImage(texture, data, false);

                if(success)
                {
                    result = ResultBuilder.Success;
                }
                else
                {
                    result = ResultBuilder.Create(ResultCode.Internal_InvalidParameter);
                    texture = null;

                    Logger.Log(LogLevel.Verbose, ":INTERNAL: Failed to parse image data.");
                }
            }

            return (result.Succeeded());
        }
#endif
        /// <summary>Generates an MD5 hash for a given byte array.</summary>
        public static string GenerateMD5(Stream data)
        {
            using(var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] hash = md5.ComputeHash(data);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>Generates an MD5 hash for a given byte array.</summary>
        public static string GenerateMD5(byte[] data)
        {
            using(var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] hash = md5.ComputeHash(data);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>Generates an MD5 hash from a given stream.</summary>
        public static async Task<string> GenerateArchiveMD5(string filepath)
        {
            using ModIOFileStream stream = DataStorage.OpenArchiveReadStream(filepath, out Result result);
            return result.Succeeded() ? await GenerateMD5Async(stream) : string.Empty;
        }

        /// <summary>Asynchronously generates an MD5 hash from a given stream.</summary>
        public static async Task<string> GenerateMD5Async(Stream stream)
        {
            // TODO: Add cancel support

            Stopwatch stopwatch = Stopwatch.StartNew();

            byte[] buffer = new byte[1024 * 1024]; // 1MB
            int bytesRead;

            using MD5 md5 = MD5.Create();

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                md5.TransformBlock(buffer, 0, bytesRead, null, 0);

                if (stopwatch.ElapsedMilliseconds < 15)
                    continue;

                await Task.Yield();
                stopwatch.Restart();
            }
            md5.TransformFinalBlock(buffer, 0, 0);

            stopwatch.Stop();

            return BitConverter.ToString(md5.Hash).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>Generates an MD5 hash for a given stream.</summary>
        public static Result GenerateMD5(Stream stream, out string MD5)
        {
            using(var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] hash = md5.ComputeHash(stream);
                string hashString = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                MD5 = hashString;
                return ResultBuilder.Success;
            }
        }

        /// <summary>Generates an MD5 hash for a given string.</summary>
        public static string GenerateMD5(string text)
        {
            return IOUtil.GenerateMD5(Encoding.UTF8.GetBytes(text));
        }

        internal static async Task<string> GetFileHashFromFilePath_md5(string filepath)
        {
            byte[] bytes = await IOUtil.GetRawBytesFromFile(filepath);

            return GenerateMD5(bytes);
        }

        // REVIEW @Jackson I need this function, should it go here?
        /// <summary>
        /// Uses an IO operation to get the raw bytes of a file and returns it as a byte[]
        /// </summary>
        internal static async Task<byte[]> GetRawBytesFromFile(string filepath)
        {
            await Task.Delay(1);
            throw new NotImplementedException();
        }

        internal static string CleanFileNameForInvalidCharacters(string filename)
        {
            // replace spaces with dashes
            string cleanedName = filename.Replace(" ", "-");

            // trim - and . from ends of string
            char[] invalidToTrim = {'-','.'};
            cleanedName = cleanedName.Trim(invalidToTrim);

            // completely remove any invalid characters
            foreach(char c in Path.GetInvalidFileNameChars())
            {
                cleanedName = cleanedName.Replace(c.ToString(), "");
            }

            return cleanedName;
        }
    }
}
