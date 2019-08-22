using System.Reflection;

namespace Pdoxcl2Sharp.Parsers
{
    internal class DecodeObject : DecodeProperty
    {
        public DecodeObject(PropertyInfo property) : base(property, TextObjectParser.PropertyType.Object)
        {
        }

        public override void Decode(ref ParadoxTextReader reader, object obj) { }
    }
}
