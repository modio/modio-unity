namespace Modio
{
    /// <summary>
    /// Specify the priority of a dependency, which controls which dependency will be used when multiple bindings exist
    /// Higher numbers are higher priority. You can modify these priorities if you want to have an even more specific override
    /// e.g. var priority = ModioServicePriority.EngineImplementation + 1;
    /// </summary>
    public enum ModioServicePriority
    {
        /// <summary>
        /// Do not bind things to this that should be used in working situations
        /// You can bind error messages at this priority to give more information to users
        /// when binding fails <see cref="ModioServices.BindErrorMessage{T}"/>
        /// </summary>
        Fallback = 0,
        
        /// <summary>
        /// Most mod.io basic implementations will use this priority
        /// </summary>
        Default = 10,
            
        /// <summary>
        /// e.g. Unity specific
        /// </summary>
        EngineImplementation = 20,
            
        /// <summary>
        /// e.g. a particular console or storefront
        /// </summary>
        PlatformProvided = 30,
        
        /// <summary>
        /// A custom implementation within your own application
        /// </summary>
        DeveloperOverride = 40,
            
        /// <summary>
        /// Intended for internal usage to support mod.io's unit testing
        /// </summary>
        UnitTestOverride = 100,
    }
}
