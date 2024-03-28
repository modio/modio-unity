using System.Collections.Generic;
using System.Linq;
using ModIO.Implementation.API.Objects;

namespace ModIO
{
    public struct TokenPack
    {
        public readonly long id;
        public readonly string name;
        public readonly string description;
        public readonly long price;
        public readonly long amount;
        public readonly Portal[] portals;

        internal TokenPack(IEnumerable<TokenPackObject> tokenPackObjects)
        {
            TokenPackObject tokenPackObject = tokenPackObjects.First();

            id = tokenPackObject.token_pack_id;
            name = tokenPackObject.name;
            description = tokenPackObject.description;
            price = tokenPackObject.price;
            amount = tokenPackObject.amount;
            portals = tokenPackObjects.Select(tpo => new Portal(tpo)).ToArray();
        }

        public struct Portal
        {
            public readonly long id;
            public readonly string portal;
            public readonly string sku;

            internal Portal(TokenPackObject tokenPackObject)
            {
                id = tokenPackObject.id;
                portal = tokenPackObject.portal;
                sku = tokenPackObject.sku;
            }
        }
    }
}
