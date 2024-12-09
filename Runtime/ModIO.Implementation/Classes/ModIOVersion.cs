namespace ModIO.Implementation
{
    /// <summary>Describes the mod.io UnityPlugin version.</summary>
    internal struct ModIOVersion : System.IComparable<ModIOVersion>
    {
        // ---------[ Singleton ]---------
        /// <summary>Singleton instance for current version.</summary>
        public static readonly ModIOVersion Current = new ModIOVersion(2024, 11, 0, "");

        // ---------[ Fields ]---------
        /// <summary>Main Version number.</summary>
        public readonly int year;

        /// <summary>Major version number.</summary>
        public readonly int month;

        /// <summary>Version build number.</summary>
        public readonly int patch;

        /// <summary>Suffix for the current version.</summary>
        public readonly string suffix;

        /// <summary>Header string containing all version information.</summary>
        readonly string headerString;

        // ---------[ Initialization ]---------
        /// <summary>Constructs an object with the given version values.</summary>
        public ModIOVersion(int year, int month, int patch, string suffix = null)
        {
            this.year = year;
            this.month = month;
            this.patch = patch;

            if(suffix == null)
            {
                suffix = string.Empty;
            }
            this.suffix = suffix;

            headerString = $"modio-{year}.{month}.{patch}{(suffix != string.Empty ? ("-" + suffix) : string.Empty)}";
        }

        // ---------[ IComparable Interface ]---------
        /// <summary>Compares the current instance with another ModIOVersion.</summary>
        public int CompareTo(ModIOVersion other)
        {
            int result = year.CompareTo(other.year);

            if(result == 0)
            {
                result = month.CompareTo(other.month);
            }
            if(result == 0)
            {
                result = patch.CompareTo(other.patch);
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
        public readonly string ToHeaderString() => headerString;


#endregion // Utility
    }
}
