using System.Reflection;
using Pdoxcl2Sharp.Converters;

namespace Pdoxcl2Sharp.Parsers
{
    internal class DecodeSetter : DecodeProperty
    {
        private readonly TextConvert _convert;

        public DecodeSetter(PropertyInfo property, PropertyType type, TextConvert convert) : base(property, type)
        {
            _convert = convert;
        }

        public override void Decode(ref ParadoxTextReader reader, object obj, ParadoxSerializerOptions options)
        {
            Property.SetValue(obj, _convert.BaseRead(ref reader, Property.PropertyType, options));
        }
    }
}
