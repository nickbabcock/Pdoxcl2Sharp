using System.Reflection;

namespace Pdoxcl2Sharp.Parsers
{
    internal class DecodeInt32 : DecodeProperty
    {
        public DecodeInt32(PropertyInfo property) : base(property, TextObjectParser.PropertyType.Scalar)
        {
        }

        public override void Decode(ref ParadoxTextReader reader, object obj) =>
            Property.SetValue(obj, reader.GetInt32());
    }
}
