
using System.Reflection;
using Pdoxcl2Sharp.Converters;

namespace Pdoxcl2Sharp.Parsers
{
    internal class DecodeDict : DecodeProperty
    {
        private readonly MethodInfo _addFn;
        private readonly TextConvert _children;

        public DecodeDict(PropertyInfo property, TextConvert children) : base(property, TextObjectParser.PropertyType.Object)
        {
            _addFn = property.PropertyType.GetMethod("Add");
            _children = children;
        }

        public override void Decode(ref ParadoxTextReader reader, object obj, ParadoxSerializerOptions options)
        {
            var child = _children.BaseRead(ref reader, typeof(int), options);
            _addFn.Invoke(obj, new[] { "abc", child });
        }
    }
}
