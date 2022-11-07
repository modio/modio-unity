using System;

namespace ModIO.Implementation.Platform
{
    /// <summary>Defines the data layout for SystemIO.</summary>
    internal static class SystemIODataLayout
    {
        /// <summary>Global Settings data structure.</summary>
        [Serializable]
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
