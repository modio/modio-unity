using System.Collections.Generic;

namespace Modio
{
    public static class Version
    {
        static readonly System.Version Current = new System.Version(2025, 6);
        static readonly List<string> EnvironmentDetails = new List<string>();

        public static void AddEnvironmentDetails(string details) => EnvironmentDetails.Add(details);

        public static string GetCurrent() =>
            EnvironmentDetails.Count == 0
                ? $"modio.cs/{Current}"
                : $"modio.cs/{Current} ({string.Join("; ", EnvironmentDetails)})";
    }
}
