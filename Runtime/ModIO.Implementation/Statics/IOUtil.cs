using System;
using System.IO;
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

        /// <summary>Encodes the texture as PNG data.</summary>
        public static byte[] GeneratePNGData(Texture2D texture)
        {
            byte[] data = null;

            if(texture == null)
            {
                data = null;

                Logger.Log(LogLevel.Verbose,
                           ":INTERNAL: Attempted to generate PNG data for NULL texture.");
            }
            else
            {
                data = ImageConversion.EncodeToPNG(texture);

                if(data == null)
                {
                    Logger.Log(LogLevel.Verbose,
                               ":INTERNAL: Failed to encode texture as PNG data.");
                }
            }

            return data;
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
        public static async Task<string> GenerateArchiveMD5Async(string filepath)
        {
            string fileHash = string.Empty;
            
            using(var stream = DataStorage.OpenArchiveReadStream(filepath, out Result result))
            {
                if(!result.Succeeded())
                {
                    return string.Empty;
                }
                ResultAnd<string> hashResult = await IOUtil.GenerateMD5Async(stream);
                fileHash = hashResult.value;
            }
            return fileHash;
        }

        /// <summary>Generates an MD5 hash for a given stream.</summary>
        public static async Task<ResultAnd<string>> GenerateMD5Async(Stream stream)
        {
            // TODO @Jackson, why is this here?
            await Task.Delay(1); // ???

            using(var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] hash = md5.ComputeHash(stream);
                string hashString = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                return ResultAnd.Create(ResultCode.Success, hashString);
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
