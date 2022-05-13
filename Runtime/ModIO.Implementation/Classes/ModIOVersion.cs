namespace ModIO.Implementation
{
    /// <summary>Describes the mod.io UnityPlugin version.</summary>
    internal struct ModIOVersion : System.IComparable<ModIOVersion>
    {
        // ---------[ Singleton ]---------
        /// <summary>Singleton instance for current version.</summary>
        public static readonly ModIOVersion Current = new ModIOVersion(4, 0, "alpha");

        // ---------[ Fields ]---------
        /// <summary>Major version number.</summary>
        /// <remarks>Represents the major version number. Increases when there is a breaking change
        /// to the interface.
        /// Changing between versions of the codebase with a different X value, will require changes
        /// to a consumer codebase in order to integrate.</remarks>
        public int version;

        /// <summary>Version build number.</summary>
        /// <remarks>Represents the build version number. Increases when a new release is created
        /// for to the Asset Store/GitHub.
        /// Changing between versions of the codebase with a different Y value, will never require
        /// changes to a consumer codebase in order to integrate, but may offer additional
        /// functionality if changes are made.</remarks>
        public int build;

        /// <summary>Suffix for the current version.</summary>
        /// <remarks>Represents additional, non-incremental version information about a build.
        /// This will never represent a difference in functionality or behaviour, but instead
        /// semantic information such as the production-readiness of a build, or the platform it was
        /// built for. Always written in lower-case, using underscore as a name break as necessary.
        /// </remarks>
        public string suffix;

        // ---------[ Initialization ]---------
        /// <summary>Constructs an object with the given version values.</summary>
        public ModIOVersion(int version = 0, int build = 0, string suffix = null)
        {
            this.version = version;
            this.build = build;

            if(suffix == null)
            {
                suffix = string.Empty;
            }
            this.suffix = suffix;
        }

        // ---------[ IComparable Interface ]---------
        /// <summary>Compares the current instance with another ModIOVersion.</summary>
        public int CompareTo(ModIOVersion other)
        {
            int result = this.version.CompareTo(other.version);

            if(result == 0)
            {
                result = this.build.CompareTo(other.build);
            }

            return result;
        }

#region Operator Overloads

        // clang-format off
        public static bool operator > (ModIOVersion a, ModIOVersion b)
        {
            return a.CompareTo(b) == 1;
        }

        public static bool operator < (ModIOVersion a, ModIOVersion b)
        {
            return a.CompareTo(b) == -1;
        }

        public static bool operator >= (ModIOVersion a, ModIOVersion b)
        {
            return a.CompareTo(b) >= 0;
        }

        public static bool operator <= (ModIOVersion a, ModIOVersion b)
        {
            return a.CompareTo(b) <= 0;
        }
        // clang-format on

#endregion // Operator Overloads

#region Utility

        /// <summary>Creates the request header representation of the version.</summary>
        public string ToHeaderString()
        {
            return $"modioUnityPlugin-{this.version.ToString()}.{this.build.ToString()}-{this.suffix}";
        }

#endregion // Utility
    }
}
