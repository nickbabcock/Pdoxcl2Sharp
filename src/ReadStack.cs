using System;
using System.Collections.Generic;
using Pdoxcl2Sharp.Parsers;

namespace Pdoxcl2Sharp
{
    internal class ReadStack
    {
        private readonly ParadoxSerializerOptions _options;
        private readonly Stack<ReadStackFrame> Frames;
        private ReadStackFrame Frame;
        private DecodeProperty Property;

        public ReadStack(ParadoxSerializerOptions options, Type root)
        {
            _options = options;

            Frame = new ReadStackFrame
            {
                ReturnValue = Activator.CreateInstance(root),
                Properties = Scratch.GetOrAddClass(root, _options)
            };

            Frames = new Stack<ReadStackFrame>();
            Frames.Push(Frame);
        }

        public TextObjectParser.PropertyType FoundProperty(in ReadOnlySpan<byte> property)
        {
            var hash = Farmhash.Sharp.Farmhash.Hash64(property);
            return Frame.Properties.TryGetValue(hash, out Property)
                ? Property.Type
                : TextObjectParser.PropertyType.None;
        }

        public void FoundValue(ref ParadoxTextReader reader)
        {
            Property.Decode(ref reader, Frame.ReturnValue);
        }

        public void Push()
        {
            var newObj = Activator.CreateInstance(Property.Property.PropertyType);
            Property.Property.SetValue(Frame.ReturnValue, newObj);
            Frame = new ReadStackFrame
            {
                ReturnValue = newObj,
                Properties = Scratch.GetOrAddClass(Property.Property.PropertyType, _options)
            };
            Frames.Push(Frame);
        }

        public void Pop()
        {
            Frames.Pop();
            Frame = Frames.Peek();
        }

        public object ReturnValue => Frame.ReturnValue;
    }
}
