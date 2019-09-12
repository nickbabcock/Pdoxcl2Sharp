using System;
using System.Reflection;
using Pdoxcl2Sharp.Converters;

namespace Pdoxcl2Sharp.Parsers
{
    internal class DecodeList : DecodeProperty
    {
        private readonly TextConvert _children;
        private readonly MethodInfo _addFn;

        public override Type ChildType { get; }

        public override PropertyType ChildFormat { get; }

        public DecodeList(PropertyInfo property, TextConvert children, Type childType, PropertyType childFormat) : base(property, PropertyType.Array)
        {
            _addFn = property.PropertyType.GetMethod("Add");
            _children = children;
            ChildType = childType;
            ChildFormat = childFormat;
        }

        public override void Decode(ref ParadoxTextReader reader, object obj, ParadoxSerializerOptions options)
        {
            var child = _children.BaseRead(ref reader, typeof(int), options);
            _addFn.Invoke(obj, new[] {child});
        }

        public override void AddChild(object obj, object nestedObj)
        {
            _addFn.Invoke(obj, new[] {nestedObj});
        }
    }
}
