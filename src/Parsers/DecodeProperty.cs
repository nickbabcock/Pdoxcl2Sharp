using System.Reflection;

namespace Pdoxcl2Sharp.Parsers
{
    internal abstract class DecodeProperty
    {
        public PropertyInfo Property { get; }
        public TextObjectParser.PropertyType Type { get; }

        internal DecodeProperty(PropertyInfo property, TextObjectParser.PropertyType type)
        {
            Property = property;
            Type = type;
        }

        public abstract void Decode(ref ParadoxTextReader reader, object obj);
    }
}
