using System.Collections.Generic;
using Modio.API.Interfaces;

namespace Modio
{
    public static class Version
    {
        static readonly System.Version Current = new System.Version(2026, 7, 0);
        static readonly HashSet<string> EnvironmentDetails = new HashSet<string>();

        public static void AddEnvironmentDetails(string details)
        {
            EnvironmentDetails.Add(details);   
        }
        
        public static void ClearEnvironmentDetails() => EnvironmentDetails.Clear();
        
        public static string GetCurrent()
        {
            string output = $"modio.cs/{Current}";
            var envDetails = new HashSet<string>(EnvironmentDetails);

            if (ModioServices.TryResolve(out IModioAPIInterface api))
                envDetails.Add(api.GetType().Name);

            if (envDetails.Count > 0)
                output = $"{output} ({string.Join("; ", envDetails)})";
            
            return output;
        }
    }
}
