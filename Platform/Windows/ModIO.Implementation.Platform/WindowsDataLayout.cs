using System;

namespace ModIO.Implementation.Platform
{
    /// <summary>Defines the data layout for Windows.</summary>
    internal static class WindowsDataLayout
    {
        /// <summary>Global Settings data structure.</summary>
        [System.Serializable]
        internal struct GlobalSettingsFile
        {
            public string RootLocalStoragePath;
        }

        /// <summary>File path for the global settings file.</summary>
        public static readonly string GlobalSettingsFilePath =
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            + @"/mod.io/globalsettings.json";

        /// <summary>Default persistent data directory.</summary>
        public static readonly string DefaultPDSDirectory =
            Environment.GetEnvironmentVariable("public") + @"/mod.io";
    }
}
