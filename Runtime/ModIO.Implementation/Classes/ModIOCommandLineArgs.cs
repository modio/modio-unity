using System.Collections.Generic;

namespace ModIO.Implementation
{
    internal static class ModIOCommandLineArgs
    {
        const string PREFIX = "-modio-";

        static Dictionary<string, string> argumentCache;

        /// <summary>
        /// Attempt to get a mod.io command line argument and its encoded value from the environment.
        /// </summary>
        /// <returns><c>true</c> if the argument was successfully found.</returns>
        /// <remarks>
        /// All arguments need to be in the format:<br/>
        /// <c>-modio-arg=value</c>
        /// </remarks>
        internal static bool TryGet(string argument, out string value)
        {
            if (argumentCache == null) GetArguments();

            return argumentCache.TryGetValue(argument, out value);
        }

        static void GetArguments()
        {
            if (argumentCache != null) return;

            argumentCache = new Dictionary<string, string>();

            string[] launchArgs = System.Environment.GetCommandLineArgs();

            foreach (string argument in launchArgs)
            {
                if (!argument.StartsWith(PREFIX)) continue;

                string[] argumentValue = argument.Split('=');

                if (argumentValue.Length != 2)
                {
                    Logger.Log(LogLevel.Warning, $"Mod.IO Launch Argument {argument} does not match format of [argument]=[value]. Ignoring argument.");
                    continue;
                }

                string key = argumentValue[0].Substring(PREFIX.Length);
                string value = argumentValue[1];

                argumentCache[key] = value;
            }
        }
    }
}
