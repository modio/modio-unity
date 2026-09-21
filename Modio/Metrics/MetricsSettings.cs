using System;

namespace Modio.Metrics
{
    [Serializable]
    public class MetricsSettings : IModioServiceSettings
    {
        public string Secret;
    }
}
