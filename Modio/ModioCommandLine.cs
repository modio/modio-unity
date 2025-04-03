using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Modio
{
    /// <summary>
    /// Allows for modio specific command line arguments to be parsed easily
    /// </summary>
    public static class ModioCommandLine
    {
        const string PREFIX = "-modio-";

        static ReadOnlyDictionary<string, string> _argumentCache;

        /// <summary>
        /// Attempt to get a mod.io command line argument and its encoded value from the environment.
        /// </summary>
        /// <returns><c>true</c> if the argument was successfully found.</returns>
        /// <remarks>
        /// All arguments need to be in the format:<br/>
        /// <c>-modio-arg=value</c>
        /// or
        /// <c>-modio-arg value</c>
        /// </remarks>
        public static bool TryGet(string argument, out string value)
        {
            if (_argumentCache == null) GetArguments();

            value = null;
            return _argumentCache != null && _argumentCache.TryGetValue(argument, out value);
        }

        static void GetArguments()
        {
            if (_argumentCache != null) return;

            var cache = new Dictionary<string, string>();
            string[] launchArgs = Environment.GetCommandLineArgs();

            for (var index = 0; index < launchArgs.Length; index++)
            {
                string argument = launchArgs[index];
                if (!argument.StartsWith(PREFIX)) continue;

                
                string[] argumentValue = argument.Split('=');

                string key;
                string value;

                //using '=' to separate key value pairs
                if (argumentValue.Length == 2)
                {
                    key = argumentValue[0].Substring(PREFIX.Length);
                    value = argumentValue[1];
                }
                else if (index + 1 < launchArgs.Length)
                {
                    key = argument.Substring(PREFIX.Length);;
                    value = launchArgs[index + 1];
                }
                else
                    continue;

                cache[key] = value;
            }

            _argumentCache = new ReadOnlyDictionary<string, string>(cache);
        }
    }
}
