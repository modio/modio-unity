using System;

namespace Modio.FileIO
{
    public interface IModioRootPathProvider
    {
        /// <summary>
        /// Path of Mod Installs
        /// </summary>
        public string Path
        {
            get;
        }
        
        /// <summary>
        /// Path of User Data
        /// </summary>
        string UserPath { get;}
    }
}
