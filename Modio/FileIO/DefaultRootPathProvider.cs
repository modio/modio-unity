using System;

namespace Modio.FileIO
{
    public class DefaultRootPathProvider : IModioRootPathProvider
    {
        public virtual string Path => $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}";
        
        public string UserPath => Path;
    }
}
