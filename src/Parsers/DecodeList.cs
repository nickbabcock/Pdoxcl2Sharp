using System;
using System.Reflection;
using Pdoxcl2Sharp.Converters;

namespace Pdoxcl2Sharp.Parsers
{
    internal class DecodeList : DecodeProperty
    {
        private readonly TextConvert _children;
        private readonly MethodInfo _addFn;

        public DecodeList(PropertyInfo property, TextConvert children) : base(property, TextObjectParser.PropertyType.Array)
        {
            _addFn = property.PropertyType.GetMethod("Add");
            _children = children;
        }

        public override void Decode(ref ParadoxTextReader reader, object obj, ParadoxSerializerOptions options)
        {
            var child = _children.BaseRead(ref reader, typeof(int), options);
            _addFn.Invoke(obj, new[] {child});
        }
    }
}
