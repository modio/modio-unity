using Debug = UnityEngine.Debug;

namespace ModIO.Implementation
{
    /// <summary>Implements utility functions during debug.</summary>
    internal static class DebugUtil
    {
        /// <summary>Runs basic asserts a path is valid.</summary>
        public static void AssertPathValid(string path, string rootDirectory)
        {
#if DEBUG
            Debug.Assert(!string.IsNullOrEmpty(path));
            Debug.Assert(path.StartsWith(rootDirectory),
                         "Path is not child of root directory:" + $"\n.path={path}"
                             + $"\n.rootDirectory={rootDirectory}");
#endif // DEBUG
        }
    }
}
