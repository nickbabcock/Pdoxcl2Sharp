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
                Properties = ParadoxSerializer.GetOrAddClass(root, _options)
            };

            Frames = new Stack<ReadStackFrame>();
            Frames.Push(Frame);
        }

        public TextObjectParser.PropertyType FoundProperty(in ReadOnlySpan<byte> property)
        {
            var hash = Farmhash.Sharp.Farmhash.Hash64(property);
            var found = Frame.Properties.TryGetValue(hash, out Property);
            if (!found)
            {
                return TextObjectParser.PropertyType.None;
            }

            switch (Property.Type)
            {
                case TextObjectParser.PropertyType.Object:
                case TextObjectParser.PropertyType.Array:
                    var current = Property.Property.GetValue(Frame.ReturnValue);
                    if (current == null)
                    {
                        var newObj = Activator.CreateInstance(Property.Property.PropertyType);
                        Property.Property.SetValue(Frame.ReturnValue, newObj);
                        current = newObj;
                    }

                    Frame = new ReadStackFrame
                    {
                        ReturnValue = current,
                        Properties = ParadoxSerializer.GetOrAddClass(Property.Property.PropertyType, _options)
                    };
                    Frames.Push(Frame);
                    break;
            }

            return Property.Type;
        }

        public void FoundValue(ref ParadoxTextReader reader)
        {
            Property.Decode(ref reader, Frame.ReturnValue, _options);
        }

        public void Push()
        {
        }

        public void Pop()
        {
            Frames.Pop();
            Frame = Frames.Peek();
        }

        public object ReturnValue => Frame.ReturnValue;
    }
}  
