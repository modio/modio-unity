using System.Collections.Generic;

namespace Modio.API
{
    public interface IApiRequest
    {
        IReadOnlyDictionary<string, object> GetBodyParameters();
    }
}
