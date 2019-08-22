using System;
using System.Collections.Generic;
using System.Reflection;

namespace Pdoxcl2Sharp.Parsers
{
    internal class DecodeString : DecodeProperty
    {
        public DecodeString(PropertyInfo property) : base(property, TextObjectParser.PropertyType.Scalar)
        {
        }

        public override void Decode(ref ParadoxTextReader reader, object obj) =>
            Property.SetValue(obj, reader.GetString());
    }
}
