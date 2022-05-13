using System;
using System.ComponentModel;
using System.Globalization;

namespace ModIO
{
    public class ModIdConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(ModId) || base.CanConvertTo(context, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture,
                                           object value)
        {
            if(value is string)
            {
                long.TryParse((string)value, out long id);
                return new ModId(id);
            }
            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture,
                                         object value, Type destinationType)
        {
            if(value is ModId)
            {
                return ((ModId)value).id.ToString();
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
