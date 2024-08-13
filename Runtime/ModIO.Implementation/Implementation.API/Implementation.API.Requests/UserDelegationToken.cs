using System;

namespace ModIO.Implementation.API.Objects
{
    [Serializable]
    public struct UserDelegationToken
    {
        public string entity;
        public string token;
    }
}
