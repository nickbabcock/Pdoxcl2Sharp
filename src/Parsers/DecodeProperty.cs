using System;
using System.Reflection;

namespace Pdoxcl2Sharp.Parsers
{
    internal abstract class DecodeProperty
    {
        public PropertyInfo Property { get; }
        public PropertyType Type { get; }
        public virtual Type ChildType { get; } = null;
        public virtual PropertyType ChildFormat { get; } = PropertyType.None;

        internal DecodeProperty(PropertyInfo property, PropertyType type)
        {
            Property = property;
            Type = type;
        }

        public abstract void Decode(ref ParadoxTextReader reader, object obj, ParadoxSerializerOptions options);

        public virtual void AddChild(object obj, object nestedObj)
        {
            throw new NotImplementedException();
        }
    }
}
