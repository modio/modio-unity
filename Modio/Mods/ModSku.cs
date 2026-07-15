using System;
using Modio.API.SchemaDefinitions;

namespace Modio.Mods
{
    public class ModSku
    {
        public string Portal { get; set; }

        public string Sku { get; set; }

        public long Id { get; set; }

        ModSku(long skuObjectId, string skuObjectSku, string skuObjectPortal)
        {
            Id = skuObjectId;
            Sku = skuObjectSku;
            Portal = skuObjectPortal;
        }

        public ModSku(ModSkuObject skuObject) : this(skuObject.Id, skuObject.Sku, skuObject.Portal) { }

        public bool Equals(ModSku other) => Portal == other.Portal && Sku == other.Sku && Id == other.Id;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            return obj.GetType() == GetType() && Equals((ModSku)obj);
        }

        public override int GetHashCode() => HashCode.Combine(Portal, Sku, Id);

        public override string ToString() => $"ModSku(Portal: {Portal}, Sku: {Sku}, Id: {Id})";
    }
}
